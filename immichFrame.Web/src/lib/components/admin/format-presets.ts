import { format } from 'date-fns';
import * as locales from 'date-fns/locale';

// A fixed sample moment: day 12 and month 8 can't be confused, and 14:30 shows 12h vs 24h.
export const SAMPLE_DATE = new Date(2025, 7, 12, 14, 30, 5);

export const CUSTOM_FORMAT = '__custom__';

/**
 * Renders a date-fns pattern the way the frame will, or returns null when the pattern is invalid.
 * `localized` mirrors the slideshow: dates use the configured language, the clock time doesn't.
 */
export function formatExample(
	pattern: string,
	language: unknown,
	localized: boolean,
	date: Date = SAMPLE_DATE
): string | null {
	const locale = localized
		? (locales[language as keyof typeof locales] as locales.Locale | undefined)
		: undefined;
	try {
		return format(date, pattern, { locale: locale ?? locales.enUS });
	} catch {
		return null;
	}
}
