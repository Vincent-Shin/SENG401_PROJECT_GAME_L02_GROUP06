-- Unemployed Simulator database schema
-- Designed from current implementation + UML plans.


-- Core identity and progression

CREATE TABLE IF NOT EXISTS players (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    username VARCHAR(32) NOT NULL UNIQUE,
    email VARCHAR(255) UNIQUE,
    score INTEGER NOT NULL DEFAULT 0 CHECK (score >= 0),
    level INTEGER NOT NULL DEFAULT 1 CHECK (level >= 1),
    xp INTEGER NOT NULL DEFAULT 0 CHECK (xp >= 0),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS player_accounts (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    player_id BIGINT NOT NULL UNIQUE REFERENCES players(id) ON DELETE CASCADE,
    password_hash VARCHAR(255) NOT NULL,
    last_login_at TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS game_sessions (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    player_id BIGINT NOT NULL REFERENCES players(id) ON DELETE CASCADE,
    started_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ended_at TIMESTAMP,
    market_state_id BIGINT,
    score_start INTEGER NOT NULL DEFAULT 0,
    score_end INTEGER,
    notes TEXT
);


-- Resume and scoring system

CREATE TABLE IF NOT EXISTS resume_profiles (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    player_id BIGINT NOT NULL UNIQUE REFERENCES players(id) ON DELETE CASCADE,
    summary TEXT,
    total_score INTEGER NOT NULL DEFAULT 0 CHECK (total_score >= 0),
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS resume_score_rules (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    rule_key VARCHAR(64) NOT NULL UNIQUE,
    points INTEGER NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    configured_by VARCHAR(64),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS certificates (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    player_id BIGINT NOT NULL REFERENCES players(id) ON DELETE CASCADE,
    title VARCHAR(120) NOT NULL,
    provider VARCHAR(120),
    issued_at DATE,
    score_delta INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS community_work (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    player_id BIGINT NOT NULL REFERENCES players(id) ON DELETE CASCADE,
    activity_name VARCHAR(120) NOT NULL,
    hours INTEGER NOT NULL DEFAULT 0 CHECK (hours >= 0),
    score_delta INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS networking_events (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    player_id BIGINT NOT NULL REFERENCES players(id) ON DELETE CASCADE,
    event_name VARCHAR(120) NOT NULL,
    contacts_made INTEGER NOT NULL DEFAULT 0 CHECK (contacts_made >= 0),
    score_delta INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS projects (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    player_id BIGINT NOT NULL REFERENCES players(id) ON DELETE CASCADE,
    title VARCHAR(120) NOT NULL,
    description TEXT,
    complexity SMALLINT NOT NULL DEFAULT 1 CHECK (complexity BETWEEN 1 AND 5),
    quality_score SMALLINT NOT NULL DEFAULT 0 CHECK (quality_score BETWEEN 0 AND 100),
    status VARCHAR(20) NOT NULL DEFAULT 'active',
    started_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    completed_at TIMESTAMP
);

CREATE TABLE IF NOT EXISTS project_updates (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    project_id BIGINT NOT NULL REFERENCES projects(id) ON DELETE CASCADE,
    update_note TEXT NOT NULL,
    score_delta INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS minigame_results (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    player_id BIGINT NOT NULL REFERENCES players(id) ON DELETE CASCADE,
    minigame_type VARCHAR(50) NOT NULL,
    result_score INTEGER NOT NULL CHECK (result_score >= 0),
    success BOOLEAN NOT NULL DEFAULT FALSE,
    played_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);


-- Hiring simulation domain

CREATE TABLE IF NOT EXISTS companies (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name VARCHAR(120) NOT NULL UNIQUE,
    tier VARCHAR(20) NOT NULL CHECK (tier IN ('startup', 'mid_tier', 'big_tech')),
    base_hire_chance NUMERIC(5,2) NOT NULL CHECK (base_hire_chance >= 0 AND base_hire_chance <= 100),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS market_states (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    phase VARCHAR(20) NOT NULL CHECK (phase IN ('boom', 'stable', 'recession')),
    hiring_modifier NUMERIC(6,3) NOT NULL DEFAULT 1.000,
    difficulty_modifier NUMERIC(6,3) NOT NULL DEFAULT 1.000,
    starts_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ends_at TIMESTAMP
);

CREATE TABLE IF NOT EXISTS applications (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    player_id BIGINT NOT NULL REFERENCES players(id) ON DELETE CASCADE,
    company_id BIGINT REFERENCES companies(id) ON DELETE SET NULL,
    market_state_id BIGINT REFERENCES market_states(id) ON DELETE SET NULL,
    message TEXT NOT NULL,
    score_snapshot INTEGER NOT NULL DEFAULT 0 CHECK (score_snapshot >= 0),
    interview_probability NUMERIC(5,2) CHECK (interview_probability >= 0 AND interview_probability <= 100),
    status VARCHAR(20) NOT NULL DEFAULT 'submitted'
        CHECK (status IN ('submitted', 'evaluated', 'interview_scheduled', 'rejected', 'offer_made', 'offer_accepted', 'offer_declined')),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS interviews (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    application_id BIGINT NOT NULL UNIQUE REFERENCES applications(id) ON DELETE CASCADE,
    scheduled_at TIMESTAMP,
    outcome VARCHAR(20) CHECK (outcome IN ('pending', 'passed', 'failed')),
    notes TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS job_offers (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    application_id BIGINT NOT NULL UNIQUE REFERENCES applications(id) ON DELETE CASCADE,
    salary INTEGER CHECK (salary >= 0),
    accepted BOOLEAN,
    decided_at TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);


-- Indexes for common lookups

CREATE INDEX IF NOT EXISTS idx_players_username ON players(username);
CREATE INDEX IF NOT EXISTS idx_sessions_player ON game_sessions(player_id, started_at DESC);
CREATE INDEX IF NOT EXISTS idx_certificates_player ON certificates(player_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_community_work_player ON community_work(player_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_networking_events_player ON networking_events(player_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_projects_player ON projects(player_id, started_at DESC);
CREATE INDEX IF NOT EXISTS idx_minigame_results_player ON minigame_results(player_id, played_at DESC);
CREATE INDEX IF NOT EXISTS idx_applications_player ON applications(player_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_applications_company ON applications(company_id, created_at DESC);
