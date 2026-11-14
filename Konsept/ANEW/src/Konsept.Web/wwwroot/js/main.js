// Sayfa giriş noktası: pasif kontroller, "Ajanı test et" ve durum yoklaması.
import { bindAgentTest } from './agent-test.js';
import { bindPassiveControls } from './passive-controls.js';
import { startPolling } from './status.js';

bindPassiveControls(document);
bindAgentTest(document);
startPolling(document);
