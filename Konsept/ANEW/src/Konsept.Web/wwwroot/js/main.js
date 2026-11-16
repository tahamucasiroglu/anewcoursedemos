// Sayfa giriş noktası: pasif kontroller, "Hata bildir", "Ajanı test et", durum yoklaması, Günlük geçmişi, tek gönderimlik çıkış formu, admin özeti, admin paneli, Ayarlar, "Ajanı eşleştir", profiller, Konseptler şeridi, düzenleme döngüsü, Silinenler, önerileri sıfırla, Konseptten Video, Üretilen Videolar ve aylık harcama.
import { loadAdminOverview } from './admin.js';
import { initAdminPanel } from './admin-panel.js';
import { initPairing } from './agent-pairing.js';
import { bindAgentTest } from './agent-test.js';
import { bindSubmitOnceForms } from './auth-form.js';
import { initConceptEdit } from './concept-edit.js';
import { initConceptWork } from './concept-work.js';
import { initConcepts } from './concepts.js';
import { initDeleted } from './deleted.js';
import { initErrorReport } from './error-report.js';
import { initJournal } from './journal.js';
import { bindPassiveControls } from './passive-controls.js';
import { initProduced } from './produced.js';
import { initProduction } from './production.js';
import { initProfiles } from './profiles.js';
import { initSettings } from './settings.js';
import { initSpend } from './spend.js';
import { initSuggestionReset } from './suggestion-reset.js';
import { startPolling } from './status.js';

bindPassiveControls(document);
bindAgentTest(document);
bindSubmitOnceForms(document);
startPolling(document);
initJournal(document);
initErrorReport(document);
loadAdminOverview(document);
initAdminPanel(document); // Admin paneli (spec 0010).
initSettings(document);
initPairing(document);
initConcepts(document); // profiles.js'in 'profile:selected' olayından önce dinlemeye başlar.
initConceptEdit(document); // Düzenleme döngüsü (spec 0015); concept-work.js'in olaylarını dinler.
initConceptWork(document); // Konseptler'in dört alanı (spec 0014); aynı olayı dinler.
initDeleted(document); // Silinenler sayfası (spec 0016).
initSuggestionReset(document); // Ayarlar → Öneriler (spec 0016).
initProduction(document); // Konseptten Video (spec 0006); 'profile:selected' olayını dinler.
initProduced(document); // Üretilen Videolar (spec 0007); 'profile:selected' ve 'agent:status' olaylarını dinler.
initProfiles(document);
initSpend(document);
