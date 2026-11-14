/**
 * Sabit 30 saniyelik kurgu ("kurgu30") — yöntem matrisi (tests/e2e/matrix.cjs) için deterministik konsept.
 *
 * Konu: İstanbul'un fethi. 4 sahne, ~51 yazılı kelime (Supertonic 3 ≈ 1,7 kelime/sn → ~30 sn).
 * Anlatım normalizasyon denemeleri içerir: "Google" (→ Gugıl), "29 Mayıs 1453" (→ yirmi dokuz Mayıs bin dört yüz elli üç),
 * "II. Mehmed" (→ İkinci Mehmed), "%40" (→ yüzde kırk), "XV. yüzyıl" (→ On beşinci yüzyıl).
 * Her sahnede görsel modelleri için İngilizce visualPrompt vardır (yazısız görsel; metin sahne kodunda çizilir).
 *
 * Tetikleme: konsept isteğinin kullanıcı promptunda "kurgu30" geçerse (ör. Fikirden üret: "kurgu30 İstanbul'un fethi")
 * mock bu konsepti döner. Konsept "kurgu30" etiketini taşır; matrix.cjs onaylı kopyayı bu etiketle bulup yeniden kullanır.
 */
'use strict';

const MARKER = 'kurgu30';

const SCENES = [
  {
    title: 'Bin yıllık surlar',
    onScreen: '29 Mayıs 1453',
    narration: '29 Mayıs 1453 sabahı, bin yıllık Konstantinopolis surları son kez derin bir sessizlikteydi.',
    visualPrompt: 'Massive ancient Byzantine stone city walls of Constantinople at dawn, mist, towers, soft golden light, no people in focus, cinematic wide shot, oil painting style, no text',
  },
  {
    title: 'Yorgun bir şehir',
    onScreen: 'XV. yüzyıl · nüfus %40 azaldı',
    narration: 'XV. yüzyıl ortasında şehrin nüfusu yüz yılda %40 azalmış, savunma iyice zayıflamıştı.',
    visualPrompt: 'Half-empty medieval city streets of 15th century Constantinople, crumbling houses, few people, overcast sky, muted warm colors, historical illustration, no text',
  },
  {
    title: 'Genç sultan',
    onScreen: 'II. Mehmed · 21 yaşında',
    narration: 'Henüz yirmi bir yaşındaki II. Mehmed, dev Şahi toplarıyla surları haftalarca dövdü.',
    visualPrompt: 'Young Ottoman sultan in a white turban and red kaftan standing beside a giant bronze cannon, smoke, siege of a stone city in the background, dramatic orange light, epic painting, no text',
  },
  {
    title: 'Bugün hâlâ orada',
    onScreen: 'Google haritasında bile',
    narration: 'Bugün Google haritasında baktığınızda bile o surların izlerini hâlâ görebilirsiniz. Bir çağ kapandı, yenisi başladı.',
    visualPrompt: 'Modern aerial view of Istanbul historic peninsula with the old Theodosian land walls, golden hour, top-down map-like perspective, clean cinematic photography, no text',
  },
];

function wordCount(t) { return (String(t).match(/\S+/g) || []).length; }

/** Konsept isteği bu kurgu için mi? */
function matches(userPrompt) { return String(userPrompt || '').toLowerCase().includes(MARKER); }

/** Sabit konsept belgesi (LLM json_schema yanıtı biçiminde). */
function concept() {
  return {
    title: 'İstanbul\'un fethi: bir çağın sonu',
    hook: '29 Mayıs 1453 sabahı bin yıllık surlar son kez sessizdi.',
    summary: 'İstanbul\'un fethini dört kısa sahnede, yaklaşık otuz saniyede anlatan sabit deneme kurgusu (kurgu30).',
    scenes: SCENES.map(s => ({
      title: s.title,
      onScreen: s.onScreen,
      visualPrompt: s.visualPrompt,
      narration: s.narration,
      durationHintSec: Math.round(wordCount(s.narration) / 1.7 * 10) / 10,
    })),
    sources: [],
    pronunciations: [{ written: 'Google', spoken: 'Gugıl' }],
    tags: [MARKER, 'tarih', 'istanbul', 'fetih', '1453'],
  };
}

module.exports = { MARKER, SCENES, matches, concept, wordCount };
