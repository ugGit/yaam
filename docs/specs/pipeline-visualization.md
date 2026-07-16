# Application Pipeline Visualization

**Date:** 2026-07-15
**Status:** Future — not yet scoped
**MVP fit:** Out of scope

## Summary

A chart view alongside the applications list that gives the user a visual read on their job search at a glance: where applications are piling up, when activity happened, and how the funnel is converting. The feature is read-only and derived entirely from existing application data — no new data entry required.

---

## User Stories

### Story 1: Status distribution chart

As a job seeker,
I want to see how many applications are in each status,
so that I can tell at a glance whether I'm spread thin or stuck at a particular stage.

**Acceptance Criteria:**
- Given I navigate to the applications view, when the pipeline visualization is visible, then I see a donut or bar chart showing the count per status (Draft, Applied, Interview scheduled, Interviewed, Offer received, Accepted, Rejected, Withdrawn).
- Given all applications are in a single status, when I view the chart, then only that segment/bar is rendered — zero-count statuses are hidden or shown as empty.
- Given I click a segment or bar, when the click registers, then the applications list filters to show only applications in that status.
- Given I have no applications, when I view the chart area, then an empty state message is shown instead of an empty chart.

---

### Story 2: Application volume over time

As a job seeker,
I want to see how many applications I submitted over time,
so that I can spot periods of high or low activity and adjust my effort accordingly.

**Acceptance Criteria:**
- Given I view the timeline chart, when it renders, then it shows a bar or line chart of applications submitted per week (or per month, depending on the date range).
- Given my application history spans more than 12 weeks, when I view the chart, then the x-axis groups by month; otherwise it groups by week.
- Given I have zero applications in a time window, when the chart renders, then that period shows as zero (no gap).
- Given I want to narrow the range, when I select a date range filter, then both the timeline and the status distribution chart update to reflect only applications in that range.

---

### Story 3: Interview and milestone timeline

As a job seeker,
I want to see key milestones (first interview scheduled, offer received) plotted on a date axis,
so that I can understand whether my pipeline has natural clusters or gaps.

**Acceptance Criteria:**
- Given I have applications with a status of "Interview scheduled" or later, when I view the milestone timeline, then those applications appear as markers on a date axis at the date the status was last set to that value.
- Given two milestones fall on the same date, when rendered, then both markers are shown without one hiding the other (stacked or offset).
- Given I hover or tap a marker, when the tooltip opens, then it shows the company name, role, and current status for that application.

---

### Story 4: Conversion funnel

As a job seeker,
I want to see the overall conversion from applications sent to interviews to offers,
so that I can measure whether my applications are landing at a reasonable rate.

**Acceptance Criteria:**
- Given I have at least one application past Draft, when I view the funnel, then it shows three stages: Applied, Interviewed (any interview status), Offers received — with counts and percentage conversion between each stage.
- Given I have zero offers, when the funnel renders, then the offer stage shows "0 (0%)" rather than being hidden.
- Given the funnel is derived from current application statuses, when I update an application's status, then the funnel recalculates on next view without a full page reload.

---

## Scope Guard

- All chart data is derived from existing `Application` records — no new data model changes needed for Stories 1–2 and 4.
- Story 3 (milestone timeline) requires knowing **when** an application entered a given status. This is not tracked in the current MVP model. Implementing Story 3 first requires adding a status-change history table or a `statusChangedAt` timestamp to `Application`. Treat this as a prerequisite for Story 3 only.
- This feature is entirely read-only — no create, update, or delete operations.
- Export (PNG, CSV) is out of scope for the initial version.
- No AI interpretation of the charts ("your interview rate is below average") — that belongs to a future analytics feature.

---

## Decisions

- **Chart library:** TBD. Candidates for Angular: Apache ECharts (ngx-echarts), Chart.js (ng2-charts), D3. Prefer a library with minimal bundle impact and good accessibility (ARIA roles, keyboard navigation).
- **Placement:** Visualization sits above or alongside the applications list on the same route — not a separate page — so the user never loses context while looking at the charts.
- **Status history:** If milestone tracking (Story 3) is implemented, status change history should be append-only and stored as a child table (`ApplicationStatusHistory`) rather than overwriting a single `statusChangedAt` field, to preserve the full progression for future analytics.
- **Date grouping:** Weekly grouping below 12 weeks of data, monthly above — keeps the timeline readable at any scale without requiring a zoom control.
