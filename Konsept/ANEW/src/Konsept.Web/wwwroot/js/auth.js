// Giriş/kayıt sayfası giriş noktası: form gönderim koruması ve ilk hatalı alana odak.
import { bindAuthForms, focusFirstInvalid } from './auth-form.js';

bindAuthForms(document);
focusFirstInvalid(document);
