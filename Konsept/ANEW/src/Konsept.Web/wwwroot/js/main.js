// Sayfa giriş noktası: pasif kontroller, "Hata bildir", "Ajanı test et", durum yoklaması, Günlük geçmişi, tek gönderimlik çıkış formu, admin özeti, Ayarlar, "Ajanı eşleştir" ve profiller.
import { loadAdminOverview } from './admin.js';
import { initPairing } from './agent-pairing.js';
import { bindAgentTest } from './agent-test.js';
import { bindSubmitOnceForms } from './auth-form.js';
import { initErrorReport } from './error-report.js';
import { initJournal } from './journal.js';
import { bindPassiveControls } from './passive-controls.js';
import { initProfiles } from './profiles.js';
import { initSettings } from './settings.js';
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
initProfiles(document);
