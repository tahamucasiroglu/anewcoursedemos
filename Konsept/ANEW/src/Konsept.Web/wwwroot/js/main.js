// Sayfa giriş noktası: pasif kontroller, "Ajanı test et", durum yoklaması, tek gönderimlik çıkış formu, admin özeti, Ayarlar, "Ajanı eşleştir" ve profiller.
import { loadAdminOverview } from './admin.js';
import { initPairing } from './agent-pairing.js';
import { bindAgentTest } from './agent-test.js';
import { bindSubmitOnceForms } from './auth-form.js';
import { bindPassiveControls } from './passive-controls.js';
import { initProfiles } from './profiles.js';
import { initSettings } from './settings.js';
import { startPolling } from './status.js';

bindPassiveControls(document);
bindAgentTest(document);
bindSubmitOnceForms(document);
startPolling(document);
loadAdminOverview(document);
initSettings(document);
initPairing(document);
initProfiles(document);
