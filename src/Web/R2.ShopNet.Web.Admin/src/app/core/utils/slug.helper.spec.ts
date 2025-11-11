import { SlugHelper } from './slug.helper';

describe('SlugHelper', () => {
  describe('generate', () => {
    it('should convert text to lowercase', () => {
      expect(SlugHelper.generate('Hello World')).toBe('hello-world');
    });

    it('should replace spaces with hyphens', () => {
      expect(SlugHelper.generate('My New Category')).toBe('my-new-category');
    });

    it('should remove special characters', () => {
      expect(SlugHelper.generate('Special!@# Characters$%^')).toBe('special-characters');
    });

    it('should handle multiple spaces', () => {
      expect(SlugHelper.generate('Multiple   Spaces')).toBe('multiple-spaces');
    });

    it('should remove leading and trailing spaces', () => {
      expect(SlugHelper.generate('  Trimmed Text  ')).toBe('trimmed-text');
    });

    it('should handle empty string', () => {
      expect(SlugHelper.generate('')).toBe('');
    });

    it('should handle already slugified text', () => {
      expect(SlugHelper.generate('already-a-slug')).toBe('already-a-slug');
    });

    it('should handle numbers', () => {
      expect(SlugHelper.generate('Category 123')).toBe('category-123');
    });

    it('should remove consecutive hyphens', () => {
      expect(SlugHelper.generate('Multiple---Hyphens')).toBe('multiple-hyphens');
    });

    it('should handle unicode characters', () => {
      expect(SlugHelper.generate('Café & Restaurant')).toBe('caf-restaurant');
    });
  });

  describe('validate', () => {
    it('should validate correct slugs', () => {
      expect(SlugHelper.validate('valid-slug')).toBe(true);
      expect(SlugHelper.validate('another-valid-slug-123')).toBe(true);
      expect(SlugHelper.validate('slug123')).toBe(true);
    });

    it('should reject invalid slugs', () => {
      expect(SlugHelper.validate('Invalid Slug')).toBe(false);
      expect(SlugHelper.validate('invalid_slug')).toBe(false);
      expect(SlugHelper.validate('-leading-hyphen')).toBe(false);
      expect(SlugHelper.validate('trailing-hyphen-')).toBe(false);
      expect(SlugHelper.validate('UPPERCASE')).toBe(false);
      expect(SlugHelper.validate('')).toBe(false);
    });
  });

  describe('sanitize', () => {
    it('should sanitize invalid slugs', () => {
      expect(SlugHelper.sanitize('Invalid Slug')).toBe('invalid-slug');
      expect(SlugHelper.sanitize('UPPERCASE')).toBe('uppercase');
      expect(SlugHelper.sanitize('-leading-hyphen-')).toBe('leading-hyphen');
    });
  });

});
