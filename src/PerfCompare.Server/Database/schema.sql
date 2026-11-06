CREATE TABLE IF NOT EXISTS environments (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    os TEXT NOT NULL,
    os_version TEXT NOT NULL,
    runtime TEXT NOT NULL,
    cores INTEGER NOT NULL CHECK (cores >= 1),
    ram_mb INTEGER NOT NULL CHECK (ram_mb >= 1),
    UNIQUE (os, os_version, runtime, cores, ram_mb)
);

CREATE TABLE IF NOT EXISTS scenarios (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS runs (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    scenario_id INTEGER NOT NULL REFERENCES scenarios (id),
    environment_id INTEGER NOT NULL REFERENCES environments (id),
    started_at TEXT NOT NULL,
    command TEXT NOT NULL,
    repeats INTEGER NOT NULL CHECK (repeats BETWEEN 1 AND 100),
    timeout_seconds INTEGER NOT NULL CHECK (timeout_seconds BETWEEN 1 AND 3600)
);

CREATE TABLE IF NOT EXISTS measurements (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    run_id INTEGER NOT NULL REFERENCES runs (id) ON DELETE CASCADE,
    iteration INTEGER NOT NULL CHECK (iteration >= 1),
    duration_ms REAL NOT NULL CHECK (duration_ms >= 0),
    peak_mem_mb REAL CHECK (peak_mem_mb IS NULL OR peak_mem_mb >= 0),
    cpu_time_ms REAL CHECK (cpu_time_ms IS NULL OR cpu_time_ms >= 0),
    exit_code INTEGER,
    status TEXT NOT NULL CHECK (status IN ('Passed', 'Failed', 'Timeout')),
    UNIQUE (run_id, iteration)
);

CREATE TABLE IF NOT EXISTS baselines (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    scenario_id INTEGER NOT NULL REFERENCES scenarios (id),
    environment_id INTEGER NOT NULL REFERENCES environments (id),
    run_id INTEGER NOT NULL REFERENCES runs (id) ON DELETE CASCADE,
    created_at TEXT NOT NULL,
    UNIQUE (scenario_id, environment_id)
);
