// Sayfa giriş noktası: pasif kontroller, "Ajanı test et", durum yoklaması, tek gönderimlik çıkış formu ve admin özeti.
import { loadAdminOverview } from './admin.js';
import { bindAgentTest } from './agent-test.js';
import { bindSubmitOnceForms } from './auth-form.js';
import { bindPassiveControls } from './passive-controls.js';
import { startPolling } from './status.js';

bindPassiveControls(document);
bindAgentTest(document);
bindSubmitOnceForms(document);
startPolling(document);
loadAdminOverview(document);
