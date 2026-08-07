/**
 * Parses a backend DateOnly string (e.g. "2026-08-07") into a local midnight Date.
 * Using `new Date(str)` treats the value as UTC and causes off-by-one errors in
 * timezones west of UTC.
 */
export function parseDateOnly(dateStr: string): Date {
  const [y, m, d] = dateStr.split('-').map(Number);
  return new Date(y, m - 1, d);
}
