import { beforeAll, beforeEach, describe, expect, it, vi } from 'vitest';
import { bindPassiveControls } from '../passive-controls.js';
import { layoutHtml } from './fixture.js';

describe('pasif kontroller (AC-3)', () => {
  beforeAll(() => {
    bindPassiveControls(document);
  });

  beforeEach(() => {
    document.body.innerHTML = layoutHtml();
  });

  it('tıklanınca hiçbir eylem çalışmaz', () => {
    const action = vi.fn();
    const control = document.getElementById('passive');
    control.addEventListener('click', action);

    control.click();

    expect(action).not.toHaveBeenCalled();
  });

  it('Enter ile de eylem çalışmaz', () => {
    const control = document.getElementById('passive');
    const event = new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true });

    control.dispatchEvent(event);

    expect(event.defaultPrevented).toBe(true);
  });

  it('erişilebilir adı ve "Yakında" ipucu vardır', () => {
    const control = document.getElementById('passive');
    expect(control.getAttribute('aria-label')).toBe('Ayarlar');
    expect(control.dataset.tooltip).toBe('Yakında');
  });

  it('pasif olmayan kontrol çalışır', () => {
    const action = vi.fn();
    const toggle = document.getElementById('theme-toggle');
    toggle.addEventListener('click', action);

    toggle.click();

    expect(action).toHaveBeenCalledOnce();
  });
});
