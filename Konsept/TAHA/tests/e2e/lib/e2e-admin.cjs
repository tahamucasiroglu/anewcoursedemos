/**
 * e2e yönetici tohumu — SAHTE yönetici hesabı. İlk çağrıda rastgele parola üretir, ASP.NET Identity v3 özeti
 * (0x01 | PRF=HMACSHA512 | 100 000 tur | 16 bayt tuz | 32 bayt alt anahtar, base64) hesaplar ve tests/e2e/out/e2e-admin.json'a
 * (git dışı) yazar. API e2e yığınında TAHAKONSEPT_ADMIN_EMAIL / TAHAKONSEPT_ADMIN_PASSWORD_HASH bu değerlerle başlatılır;
 * böylece gerçek .env yöneticisi e2e veritabanına asla girmez. Gerçek özet hiçbir yerde kullanılmaz.
 */
'use strict';
const crypto = require('crypto');
const fs = require('fs');
const path = require('path');

const FILE = path.join(__dirname, '..', 'out', 'e2e-admin.json');

function identityV3Hash(password) {
  const salt = crypto.randomBytes(16);
  const subkey = crypto.pbkdf2Sync(Buffer.from(password, 'utf8'), salt, 100000, 32, 'sha512');
  const header = Buffer.alloc(13);
  header.writeUInt8(0x01, 0);
  header.writeUInt32BE(2, 1); // KeyDerivationPrf.HMACSHA512
  header.writeUInt32BE(100000, 5);
  header.writeUInt32BE(salt.length, 9);
  return Buffer.concat([header, salt, subkey]).toString('base64');
}

/** @returns {{ email: string, password: string, hash: string }} */
function e2eAdmin() {
  if (fs.existsSync(FILE)) { return JSON.parse(fs.readFileSync(FILE, 'utf8')); }
  const password = 'e2e-' + crypto.randomBytes(12).toString('base64url');
  const admin = { email: 'e2e-admin@e2e.local', password, hash: identityV3Hash(password) };
  fs.mkdirSync(path.dirname(FILE), { recursive: true });
  fs.writeFileSync(FILE, JSON.stringify(admin, null, 2));
  return admin;
}

module.exports = { e2eAdmin, identityV3Hash };
