-------------------------------------------------------------------------------
-- TR-02 Season calendar to back SeasonGrain boundary lookups
-------------------------------------------------------------------------------

create table ${schema}.daily_login_bonus_xp_seasons (
    season_id bigint generated always as identity primary key,
    label text not null,
    start_date date not null,
    end_date date not null,
    created_at timestamptz not null default ${schema}.now_utc(),
    constraint daily_login_bonus_xp_seasons_date_window_chk check (end_date >= start_date)
);

create index daily_login_bonus_xp_seasons_start_end_idx
    on ${schema}.daily_login_bonus_xp_seasons (start_date, end_date);

comment on table ${schema}.daily_login_bonus_xp_seasons is 'Season calendar defining hard streak reset boundaries.';
comment on column ${schema}.daily_login_bonus_xp_seasons.season_id is 'Surrogate identifier for the season window.';
comment on column ${schema}.daily_login_bonus_xp_seasons.label is 'Operator-facing label for the season (e.g., Season 1).';
comment on column ${schema}.daily_login_bonus_xp_seasons.start_date is 'UTC date when the season starts.';
comment on column ${schema}.daily_login_bonus_xp_seasons.end_date is 'UTC date when the season ends (inclusive).';
comment on column ${schema}.daily_login_bonus_xp_seasons.created_at is 'Timestamp when the season row was created.';
