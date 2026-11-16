// Sayfa giriş noktası: pasif kontroller, "Hata bildir", "Ajanı test et", durum yoklaması, Günlük geçmişi, tek gönderimlik çıkış formu, admin özeti, Ayarlar, "Ajanı eşleştir", profiller, Konseptler şeridi ve aylık harcama.
import { loadAdminOverview } from './admin.js';
import { initPairing } from './agent-pairing.js';
import { bindAgentTest } from './agent-test.js';
import { bindSubmitOnceForms } from './auth-form.js';
import { initConceptWork } from './concept-work.js';
import { initConcepts } from './concepts.js';
import { initErrorReport } from './error-report.js';
import { initJournal } from './journal.js';
import { bindPassiveControls } from './passive-controls.js';
import { initProfiles } from './profiles.js';
import { initSettings } from './settings.js';
import { initSpend } from './spend.js';
import { startPolling } from './status.js';

bindPassiveControls(document);
bindAgentTest(document);
bindSubmitOnceForms(document);
startPolling(document);
initJournal(document);
initErrorReport(document);
loadAdminOverview(document);
initSettings(document);
initPairing(document);
initConcepts(document); // profiles.js'in 'profile:selected' olayından önce dinlemeye başlar.
initConceptWork(document); // Konseptler'in dört alanı (spec 0014); aynı olayı dinler.
initProfiles(document);
initSpend(document);
