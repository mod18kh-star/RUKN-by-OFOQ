export function developmentMfaBypassEnabled() {
  /*
   * Vite replaces import.meta.env.DEV at build time.
   * Production builds always evaluate this to false.
   */
  return import.meta.env.DEV;
}
