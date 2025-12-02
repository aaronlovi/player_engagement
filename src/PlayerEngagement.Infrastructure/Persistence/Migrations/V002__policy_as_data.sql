-------------------------------------------------------------------------------
-- TR-01 Policy-as-Data schema extensions
-------------------------------------------------------------------------------

create table ${schema}.daily_login_bonus_xp_policies (
    policy_key text primary key,
    policy_id bigint not null,
    display_name text not null,
    description text null,
    created_at timestamptz not null default ${schema}.now_utc(),
    created_by text not null
);

comment on table ${schema}.daily_login_bonus_xp_policies is 'Registry of policy identifiers (e.g., daily-login-xp) managed via Policy-as-Data.';
comment on column ${schema}.daily_login_bonus_xp_policies.policy_key is 'Stable slug identifier for the policy.';
comment on column ${schema}.daily_login_bonus_xp_policies.policy_id is 'Surrogate identifier supplied by DbmService.GetNextId64().';
comment on column ${schema}.daily_login_bonus_xp_policies.display_name is 'Operator-facing display name.';
comment on column ${schema}.daily_login_bonus_xp_policies.description is 'Optional description of the policy purpose.';
comment on column ${schema}.daily_login_bonus_xp_policies.created_at is 'Timestamp when the policy shell was created.';
comment on column ${schema}.daily_login_bonus_xp_policies.created_by is 'Operator or system that created the policy shell.';

create unique index daily_login_bonus_xp_policies_policy_id_uidx on ${schema}.daily_login_bonus_xp_policies (policy_id);

-------------------------------------------------------------------------------

create table ${schema}.daily_login_bonus_xp_policy_versions (
    policy_key text not null,
    policy_version bigint not null,
    status text not null check (status in ('Draft', 'Published', 'Archived')),
    base_xp_amount int not null check (base_xp_amount > 0),
    currency text not null check (currency ~ '^[A-Z]{3,8}$'),
    claim_window_start_minutes int not null check (claim_window_start_minutes >= 0 and claim_window_start_minutes < 1440),
    claim_window_duration_hours int not null check (claim_window_duration_hours >= 1 and claim_window_duration_hours <= 24),
    anchor_strategy text not null check (anchor_strategy in ('AnchorTimezone', 'FixedUtc', 'ServerLocal')),
    grace_allowed_misses int not null check (grace_allowed_misses >= 0),
    grace_window_days int not null check (grace_window_days >= grace_allowed_misses),
    streak_model_type text not null check (streak_model_type in ('PlateauCap', 'WeeklyCycleReset', 'DecayCurve', 'TieredSeasonalReset', 'MilestoneMetaReward')),
    streak_model_parameters jsonb not null default '{}'::jsonb,
    preview_sample_window_days int not null default 14 check (preview_sample_window_days >= 1),
    preview_default_segment text null,
    seasonal_metadata jsonb not null default '{}'::jsonb,
    effective_at timestamptz null,
    superseded_at timestamptz null,
    created_at timestamptz not null default ${schema}.now_utc(),
    created_by text not null,
    published_at timestamptz null,
    constraint daily_login_bonus_xp_policy_versions_pk primary key (policy_key, policy_version),
    constraint daily_login_bonus_xp_policy_versions_policy_fk foreign key (policy_key) references ${schema}.daily_login_bonus_xp_policies (policy_key) on delete cascade
);

create unique index daily_login_bonus_xp_policy_versions_published_uidx
    on ${schema}.daily_login_bonus_xp_policy_versions (policy_key)
    where status = 'Published';

comment on table ${schema}.daily_login_bonus_xp_policy_versions is 'Immutable policy version records (draft/published/archived).';
comment on column ${schema}.daily_login_bonus_xp_policy_versions.policy_key is 'Foreign key to daily_login_bonus_xp_policies.';
comment on column ${schema}.daily_login_bonus_xp_policy_versions.policy_version is 'Monotonic version number for a given policy.';
comment on column ${schema}.daily_login_bonus_xp_policy_versions.status is 'Draft, Published, or Archived state.';
comment on column ${schema}.daily_login_bonus_xp_policy_versions.base_xp_amount is 'Base XP granted before multipliers.';
comment on column ${schema}.daily_login_bonus_xp_policy_versions.currency is 'Currency of the base award (XP for now).';
comment on column ${schema}.daily_login_bonus_xp_policy_versions.claim_window_start_minutes is 'Minutes offset from midnight in the anchor timezone when claim window starts.';
comment on column ${schema}.daily_login_bonus_xp_policy_versions.claim_window_duration_hours is 'Duration of the claim window in hours.';
comment on column ${schema}.daily_login_bonus_xp_policy_versions.anchor_strategy is 'Strategy used to determine reward-day boundaries.';
comment on column ${schema}.daily_login_bonus_xp_policy_versions.grace_allowed_misses is 'Maximum number of missed days allowed within the grace window.';
comment on column ${schema}.daily_login_bonus_xp_policy_versions.grace_window_days is 'Window in days used when calculating grace usage.';
comment on column ${schema}.daily_login_bonus_xp_policy_versions.streak_model_type is 'Policy streak model type (Plateau, Weekly Reset, Decay, etc.).';
comment on column ${schema}.daily_login_bonus_xp_policy_versions.streak_model_parameters is 'Model-specific configuration payload.';
comment on column ${schema}.daily_login_bonus_xp_policy_versions.preview_sample_window_days is 'Default window for preview simulations.';
comment on column ${schema}.daily_login_bonus_xp_policy_versions.preview_default_segment is 'Optional default segment for preview calculations.';
comment on column ${schema}.daily_login_bonus_xp_policy_versions.seasonal_metadata is 'Optional metadata to support UI presentation of seasonal boosts.';
comment on column ${schema}.daily_login_bonus_xp_policy_versions.effective_at is 'Timestamp when the version becomes active.';
comment on column ${schema}.daily_login_bonus_xp_policy_versions.superseded_at is 'Timestamp when the version was superseded.';
comment on column ${schema}.daily_login_bonus_xp_policy_versions.created_at is 'Timestamp when the version row was created.';
comment on column ${schema}.daily_login_bonus_xp_policy_versions.created_by is 'Operator or system that created the version.';
comment on column ${schema}.daily_login_bonus_xp_policy_versions.published_at is 'Timestamp when the version was published.';

-------------------------------------------------------------------------------

create table ${schema}.daily_login_bonus_xp_policy_streak_curve (
    streak_curve_id bigint not null,
    policy_key text not null,
    policy_version bigint not null,
    day_index int not null check (day_index >= 0),
    multiplier numeric(8,4) not null check (multiplier >= 0),
    additive_bonus_xp int not null check (additive_bonus_xp >= 0),
    cap_next_day boolean not null default false,
    constraint daily_login_bonus_xp_policy_streak_curve_pk primary key (streak_curve_id),
    constraint daily_login_bonus_xp_policy_streak_curve_key_idx unique (policy_key, policy_version, day_index),
    constraint daily_login_bonus_xp_policy_streak_curve_version_fk foreign key (policy_key, policy_version)
        references ${schema}.daily_login_bonus_xp_policy_versions (policy_key, policy_version) on delete cascade
);

comment on table ${schema}.daily_login_bonus_xp_policy_streak_curve is 'Streak curve entries defining multipliers and bonuses per day index.';
comment on column ${schema}.daily_login_bonus_xp_policy_streak_curve.streak_curve_id is 'Surrogate identifier supplied by DbmService.GetNextId64().';
comment on column ${schema}.daily_login_bonus_xp_policy_streak_curve.policy_key is 'Foreign key to daily_login_bonus_xp_policy_versions.';
comment on column ${schema}.daily_login_bonus_xp_policy_streak_curve.policy_version is 'Policy version the streak entry belongs to.';
comment on column ${schema}.daily_login_bonus_xp_policy_streak_curve.day_index is 'Zero-based streak day index.';
comment on column ${schema}.daily_login_bonus_xp_policy_streak_curve.multiplier is 'Multiplier applied to the base XP for this day.';
comment on column ${schema}.daily_login_bonus_xp_policy_streak_curve.additive_bonus_xp is 'Additive XP bonus for this day.';
comment on column ${schema}.daily_login_bonus_xp_policy_streak_curve.cap_next_day is 'Flag indicating that growth should plateau after this day.';

-------------------------------------------------------------------------------

create extension if not exists btree_gist;

create table ${schema}.daily_login_bonus_xp_policy_seasonal_boosts (
    policy_key text not null,
    policy_version bigint not null,
    boost_id bigint not null primary key,
    label text not null,
    multiplier numeric(8,4) not null check (multiplier >= 1),
    start_utc timestamptz not null,
    end_utc timestamptz not null check (end_utc > start_utc),
    constraint daily_login_bonus_xp_policy_seasonal_boosts_version_fk foreign key (policy_key, policy_version)
        references ${schema}.daily_login_bonus_xp_policy_versions (policy_key, policy_version) on delete cascade
);

create index daily_login_bonus_xp_policy_seasonal_boosts_timing_idx
    on ${schema}.daily_login_bonus_xp_policy_seasonal_boosts (policy_key, policy_version, start_utc, end_utc);

create index daily_login_bonus_xp_policy_seasonal_boosts_label_idx
    on ${schema}.daily_login_bonus_xp_policy_seasonal_boosts (policy_key, policy_version, label);

alter table ${schema}.daily_login_bonus_xp_policy_seasonal_boosts
    add constraint daily_login_bonus_xp_policy_seasonal_boosts_no_overlap
    exclude using gist (
        policy_key with =,
        policy_version with =,
        tstzrange(start_utc, end_utc) with &&
    );

comment on table ${schema}.daily_login_bonus_xp_policy_seasonal_boosts is 'Seasonal or event multipliers applied to policy versions.';
comment on column ${schema}.daily_login_bonus_xp_policy_seasonal_boosts.policy_key is 'Foreign key to daily_login_bonus_xp_policy_versions.';
comment on column ${schema}.daily_login_bonus_xp_policy_seasonal_boosts.policy_version is 'Policy version receiving the boost.';
comment on column ${schema}.daily_login_bonus_xp_policy_seasonal_boosts.boost_id is 'Identifier for the seasonal boost row.';
comment on column ${schema}.daily_login_bonus_xp_policy_seasonal_boosts.label is 'Operator-defined label for the boost.';
comment on column ${schema}.daily_login_bonus_xp_policy_seasonal_boosts.multiplier is 'Multiplier applied during the boost window.';
comment on column ${schema}.daily_login_bonus_xp_policy_seasonal_boosts.start_utc is 'UTC start timestamp for the boost.';
comment on column ${schema}.daily_login_bonus_xp_policy_seasonal_boosts.end_utc is 'UTC end timestamp for the boost.';

-------------------------------------------------------------------------------

create table ${schema}.daily_login_bonus_xp_policy_segment_overrides (
    override_id bigint not null,
    segment_key text not null,
    policy_key text not null,
    target_policy_version bigint not null,
    created_at timestamptz not null default ${schema}.now_utc(),
    created_by text not null,
    constraint daily_login_bonus_xp_policy_segment_overrides_pk primary key (override_id),
    constraint dlb_xp_policy_segment_overrides_seg_uidx unique (segment_key, policy_key),
    constraint daily_login_bonus_xp_policy_segment_overrides_policy_fk foreign key (policy_key) references ${schema}.daily_login_bonus_xp_policies (policy_key) on delete cascade,
    constraint daily_login_bonus_xp_policy_segment_overrides_version_fk foreign key (policy_key, target_policy_version)
        references ${schema}.daily_login_bonus_xp_policy_versions (policy_key, policy_version) on delete cascade
);

comment on table ${schema}.daily_login_bonus_xp_policy_segment_overrides is 'Maps player segments to alternate policy versions.';
comment on column ${schema}.daily_login_bonus_xp_policy_segment_overrides.override_id is 'Surrogate identifier supplied by DbmService.GetNextId64().';
comment on column ${schema}.daily_login_bonus_xp_policy_segment_overrides.segment_key is 'Segment identifier resolved at claim time.';
comment on column ${schema}.daily_login_bonus_xp_policy_segment_overrides.policy_key is 'Policy key associated with the override.';
comment on column ${schema}.daily_login_bonus_xp_policy_segment_overrides.target_policy_version is 'Policy version applied to the segment.';
comment on column ${schema}.daily_login_bonus_xp_policy_segment_overrides.created_at is 'Timestamp when the override was created.';
comment on column ${schema}.daily_login_bonus_xp_policy_segment_overrides.created_by is 'Operator who created the override.';

-------------------------------------------------------------------------------

drop table if exists ${schema}.daily_login_bonus_xp_rules;
