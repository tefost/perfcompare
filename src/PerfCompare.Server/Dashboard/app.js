"use strict";

const palette = ["#3b82f6", "#f97316", "#10b981", "#a855f7", "#ef4444", "#14b8a6"];

const metricNames = {
  durationMedianMs: "медіана часу",
  durationP95Ms: "P95 часу",
  meanPeakMemMb: "пік пам'яті",
  meanCpuTimeMs: "процесорний час",
  successRate: "частка успіху"
};

const elements = {
  scenario: document.getElementById("scenario"),
  threshold: document.getElementById("threshold"),
  exportLink: document.getElementById("export"),
  reportLink: document.getElementById("report"),
  message: document.getElementById("message"),
  compareBody: document.querySelector("#compare-table tbody"),
  runsBody: document.querySelector("#runs-table tbody"),
  compareCanvas: document.getElementById("compare-chart"),
  trendCanvas: document.getElementById("trend-chart")
};

let compareChart = null;
let trendChart = null;
let renderVersion = 0;

async function getJson(url) {
  const response = await fetch(url);
  if (!response.ok) {
    throw new Error(`${url}: ${response.status}`);
  }

  return response.json();
}

function setMessage(text) {
  elements.message.textContent = text;
  elements.message.hidden = !text;
}

function format(value, digits) {
  return value === null || value === undefined ? "-" : Number(value).toFixed(digits);
}

function environmentLabel(environment) {
  return `${environment.os} ${environment.osVersion}, ${environment.cores} ядер, ${environment.ramMb} МБ`;
}

function dateLabel(iso) {
  return new Date(iso).toLocaleString("uk-UA");
}

function textCell(text) {
  const cell = document.createElement("td");
  cell.textContent = text;
  return cell;
}

function elementCell(child) {
  const cell = document.createElement("td");
  cell.appendChild(child);
  return cell;
}

function regressionBadge(report) {
  const badge = document.createElement("span");
  if (report.status === "regression") {
    const parts = report.metrics
      .filter((metric) => metric.isRegression)
      .map((metric) => `${metricNames[metric.metric] || metric.metric} ${metric.changePercent > 0 ? "+" : ""}${metric.changePercent}%`);
    badge.className = "badge bad";
    badge.textContent = `регресія: ${parts.join(", ")}`;
  } else if (report.status === "ok") {
    badge.className = "badge ok";
    badge.textContent = "в нормі";
  } else {
    badge.className = "badge none";
    badge.textContent = "немає базової лінії";
  }

  return badge;
}

function renderCompareTable(items, reports) {
  elements.compareBody.replaceChildren();
  items.forEach((item, index) => {
    const duration = item.stats.duration;
    const row = document.createElement("tr");
    row.appendChild(textCell(environmentLabel(item.environment)));
    row.appendChild(textCell(`#${item.runId}, ${dateLabel(item.startedAt)}`));
    row.appendChild(textCell(format(duration && duration.median, 1)));
    row.appendChild(textCell(format(duration && duration.p95, 1)));
    row.appendChild(textCell(format(item.stats.stabilityIndex, 1)));
    row.appendChild(textCell(`${format(item.stats.successRate * 100, 0)}%`));
    row.appendChild(textCell(item.slowdownVsFastest === null ? "-" : `×${format(item.slowdownVsFastest, 2)}`));
    row.appendChild(textCell(item.baselineRunId === null ? "-" : `#${item.baselineRunId}`));
    row.appendChild(elementCell(regressionBadge(reports[index])));
    elements.compareBody.appendChild(row);
  });
}

function drawChart(previous, canvas, config) {
  if (previous) {
    previous.destroy();
  }

  return new Chart(canvas, config);
}

function renderCompareChart(items) {
  const labels = items.map((item) => environmentLabel(item.environment));
  compareChart = drawChart(compareChart, elements.compareCanvas, {
    type: "bar",
    data: {
      labels,
      datasets: [
        { label: "Медіана, мс", data: items.map((item) => item.stats.duration && item.stats.duration.median), backgroundColor: palette[0] },
        { label: "P95, мс", data: items.map((item) => item.stats.duration && item.stats.duration.p95), backgroundColor: palette[1] }
      ]
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      scales: { y: { beginAtZero: true, title: { display: true, text: "мс" } } }
    }
  });
}

async function renderTrend(runs, items) {
  const recent = runs.slice().sort((a, b) => a.id - b.id).slice(-30);
  const stats = await Promise.all(recent.map((run) => getJson(`api/runs/${run.id}/stats`)));
  const labelsById = new Map(items.map((item) => [item.environmentId, environmentLabel(item.environment)]));
  const environmentIds = [...new Set(recent.map((run) => run.environmentId))];

  const datasets = environmentIds.map((environmentId, index) => ({
    label: labelsById.get(environmentId) || `середовище ${environmentId}`,
    data: recent.map((run, position) =>
      run.environmentId === environmentId && stats[position].duration ? stats[position].duration.median : null),
    borderColor: palette[index % palette.length],
    backgroundColor: palette[index % palette.length],
    spanGaps: true,
    tension: 0.2
  }));

  trendChart = drawChart(trendChart, elements.trendCanvas, {
    type: "line",
    data: { labels: recent.map((run) => `#${run.id}`), datasets },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      scales: { y: { beginAtZero: true, title: { display: true, text: "медіана, мс" } } }
    }
  });
}

async function makeBaseline(runId, button) {
  button.disabled = true;
  const response = await fetch("api/baselines", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ runId })
  });

  if (!response.ok) {
    setMessage(`Не вдалося позначити базову лінію: ${response.status}`);
    button.disabled = false;
    return;
  }

  await render();
}

function renderRuns(runs, items) {
  const baselineIds = new Set(items.map((item) => item.baselineRunId).filter((id) => id !== null));
  const labelsById = new Map(items.map((item) => [item.environmentId, environmentLabel(item.environment)]));

  elements.runsBody.replaceChildren();
  runs.slice().sort((a, b) => b.id - a.id).slice(0, 50).forEach((run) => {
    const row = document.createElement("tr");
    row.appendChild(textCell(`#${run.id}`));
    row.appendChild(textCell(labelsById.get(run.environmentId) || `${run.os} (середовище ${run.environmentId})`));
    row.appendChild(textCell(dateLabel(run.startedAt)));
    row.appendChild(textCell(String(run.measurements)));

    if (baselineIds.has(run.id)) {
      row.appendChild(textCell("базова"));
    } else {
      const button = document.createElement("button");
      button.type = "button";
      button.textContent = "Зробити базовою";
      button.addEventListener("click", () => makeBaseline(run.id, button));
      row.appendChild(elementCell(button));
    }

    elements.runsBody.appendChild(row);
  });
}

async function render() {
  const scenarioId = elements.scenario.value;
  if (!scenarioId) {
    return;
  }

  const version = ++renderVersion;
  elements.exportLink.href = `api/export.csv?scenarioId=${scenarioId}`;
  elements.reportLink.href = `api/report.html?scenarioId=${scenarioId}&threshold=${Number(elements.threshold.value) || 10}`;
  setMessage("");

  try {
    const compare = await getJson(`api/compare?scenarioId=${scenarioId}`);
    const runs = (await getJson("api/runs")).filter((run) => run.scenario === compare.scenario);
    const threshold = Number(elements.threshold.value) || 10;
    const reports = await Promise.all(
      compare.items.map((item) => getJson(`api/regressions?runId=${item.runId}&threshold=${threshold}`)));
    if (version !== renderVersion) {
      return;
    }

    renderCompareTable(compare.items, reports);
    renderCompareChart(compare.items);
    renderRuns(runs, compare.items);
    await renderTrend(runs, compare.items);
  } catch (error) {
    setMessage(`Не вдалося завантажити дані: ${error.message}`);
  }
}

async function start() {
  if (typeof Chart === "undefined") {
    setMessage("Бібліотека графіків не завантажилась.");
    return;
  }

  Chart.defaults.color = getComputedStyle(document.body).color;
  Chart.defaults.borderColor = "rgba(128, 128, 128, 0.25)";

  try {
    const scenarios = await getJson("api/scenarios");
    if (scenarios.length === 0) {
      setMessage("Даних ще немає. Запустіть runner з параметром --server і оновіть сторінку.");
      return;
    }

    scenarios.forEach((scenario) => {
      const option = document.createElement("option");
      option.value = scenario.id;
      option.textContent = scenario.name;
      elements.scenario.appendChild(option);
    });

    elements.scenario.addEventListener("change", render);
    elements.threshold.addEventListener("change", render);
    await render();
  } catch (error) {
    setMessage(`Не вдалося завантажити сценарії: ${error.message}`);
  }
}

start();
