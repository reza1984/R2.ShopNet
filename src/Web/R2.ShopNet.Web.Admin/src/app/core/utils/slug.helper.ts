/**
 * Utility class for generating and validating URL-friendly slugs
 */
export class SlugHelper {

  /**
   * Generates a URL-friendly slug from a given text
   * @param text The text to convert to a slug
   * @returns A lowercase, hyphenated slug string
   */
  static generate(text: string): string {
    if (!text) {
      return '';
    }

    text =text
      .toLowerCase()
      .trim()
      // Replace spaces with hyphens
      .replace(/\s+/g, '-')
      // Remove special characters except hyphens
      .replace(/[^\w\-]+/g, '')
      // Replace multiple hyphens with single hyphen
      .replace(/\-\-+/g, '-')
      // Remove leading and trailing hyphens
      .replace(/^-+/, '')
      .replace(/-+$/, '');

    return text;
  }

  /**
   * Validates if a string is a valid slug
   * @param slug The slug to validate
   * @returns True if the slug is valid, false otherwise
   */
  static validate(slug: string): boolean {
    if (!slug) return false;

    // A valid slug should only contain lowercase letters, numbers, and hyphens
    // Should not start or end with a hyphen
    const slugPattern = /^[a-z0-9]+(?:-[a-z0-9]+)*$/;
    return slugPattern.test(slug);
  }

  /**
   * Sanitizes an existing slug to ensure it meets slug requirements
   * @param slug The slug to sanitize
   * @returns A sanitized slug string
   */
  static sanitize(slug: string): string {
    return this.generate(slug);
  }

}
