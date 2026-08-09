import type { UpcomingItem } from "@/lib/api/types";
import { downloadTextFile, stampFilename } from "@/lib/reports/export-csv";

function pad2(n: number): string {
  return String(n).padStart(2, "0");
}

/** ICS date VALUE (all-day): YYYYMMDD */
function toIcsDate(isoDate: string): string {
  const d = isoDate.slice(0, 10).replace(/-/g, "");
  return d.length === 8 ? d : d;
}

function escapeIcsText(s: string): string {
  return s
    .replace(/\\/g, "\\\\")
    .replace(/;/g, "\\;")
    .replace(/,/g, "\\,")
    .replace(/\n/g, "\\n");
}

/**
 * Build a VCALENDAR with one VEVENT per upcoming renewal (task 16.3.1).
 */
export function buildUpcomingIcs(
  items: UpcomingItem[],
  options?: { calendarName?: string; productId?: string },
): string {
  const calName = options?.calendarName ?? "Subify renewals";
  const prod = options?.productId ?? "-//Subify OS//EN";
  const stamp = new Date();
  const dtStamp = `${stamp.getUTCFullYear()}${pad2(stamp.getUTCMonth() + 1)}${pad2(stamp.getUTCDate())}T${pad2(stamp.getUTCHours())}${pad2(stamp.getUTCMinutes())}${pad2(stamp.getUTCSeconds())}Z`;

  const events = items.map((item) => {
    const day = toIcsDate(item.nextRenewalDate);
    const uid = `${item.id}@subify.local`;
    const amount =
      item.userShare != null
        ? `${item.userShare} ${item.currency}`
        : `${item.price} ${item.currency}`;
    const summary = escapeIcsText(`${item.name} · ${amount}`);
    const desc = escapeIcsText(
      [
        item.isOverdue ? "Overdue" : "Upcoming renewal",
        `Share: ${amount}`,
        `Days: ${item.daysUntilRenewal}`,
      ].join("\\n"),
    );

    return [
      "BEGIN:VEVENT",
      `UID:${uid}`,
      `DTSTAMP:${dtStamp}`,
      `DTSTART;VALUE=DATE:${day}`,
      `DTEND;VALUE=DATE:${day}`,
      `SUMMARY:${summary}`,
      `DESCRIPTION:${desc}`,
      "END:VEVENT",
    ].join("\r\n");
  });

  return [
    "BEGIN:VCALENDAR",
    "VERSION:2.0",
    `PRODID:${prod}`,
    "CALSCALE:GREGORIAN",
    "METHOD:PUBLISH",
    `X-WR-CALNAME:${escapeIcsText(calName)}`,
    ...events,
    "END:VCALENDAR",
    "",
  ].join("\r\n");
}

export function downloadUpcomingIcs(
  items: UpcomingItem[],
  calendarName?: string,
): void {
  const ics = buildUpcomingIcs(items, { calendarName });
  downloadTextFile(
    stampFilename("subify-renewals", "ics"),
    ics,
    "text/calendar;charset=utf-8",
  );
}
