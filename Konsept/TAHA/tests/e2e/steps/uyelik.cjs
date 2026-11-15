/**
 * Adım "uyelik" — üyelik sistemi uçtan uca (her kullanıcı ayrı gizli tarayıcı bağlamında):
 *  1. Oturumsuz: / → Giriş, /Kurulum → Kayıt ol; giriş ↔ kayıt bağlantıları; açık/koyu tema ve 390 px ekran görüntüleri.
 *  2. A kayıt olur → Başlangıç; profil ekler; çıkış → girişle tekrar girer.
 *  3. B kayıt olur → A'nın profilini listede görmez, kimlikle de açamaz/silemez (404); Yönetim paneline giremez.
 *  4. SAHTE e2e yöneticisi (out/e2e-admin.json) normal giriş sayfasından e-postayla girer → Kullanıcılar sekmesi:
 *     A ve B listede (normal kullanıcı); "Yeni kayıtlara izin ver" kapatılır → Kayıt ol sayfası mesaj gösterir, Ajax kayıt 403;
 *     tekrar açılır. B devre dışı bırakılır → B giriş yapamaz; etkinleştirilir → girer.
 */
'use strict';
const { e2eAdmin } = require('../lib/e2e-admin.cjs');

module.exports = async (ctx) => {
  const { base, check, sleep } = ctx;
  const stamp = Date.now().toString(36);
  const userA = 'e2e_a_' + stamp;
  const userB = 'e2e_b_' + stamp;
  const pass = 'Parola-' + stamp + '!';
  const contexts = [];

  async function fresh(width, height) {
    const bc = await ctx.browser.createBrowserContext();
    contexts.push(bc);
    const p = await bc.newPage();
    await p.setViewport({ width: width || 1280, height: height || 900 });
    p.on('dialog', d => d.accept().catch(() => {}));
    return p;
  }
  async function go(p, url) { await p.goto(base + url, { waitUntil: 'networkidle2' }); await sleep(500); }
  async function shot(p, name) { const f = require('path').join(ctx.out, `uyelik-${name}.png`); await p.screenshot({ path: f, fullPage: true }); ctx.log('📷', `uyelik-${name}.png`); }
  async function setTheme(p, theme) {
    await p.evaluate(t => { document.documentElement.setAttribute('data-theme', t); try { localStorage.setItem('tk-theme', t); } catch (e) { /* yok */ } }, theme);
    await sleep(200);
  }
  async function submit(p, formSel) {
    await Promise.all([p.waitForNavigation({ waitUntil: 'networkidle2', timeout: 15000 }).catch(() => null), p.click(formSel + ' button[type=submit]')]);
    await sleep(800);
  }
  async function register(p, name) {
    await go(p, '/Account/Register');
    await p.type('#register-user', name);
    await p.type('#register-pass', pass);
    await p.type('#register-pass2', pass);
    await submit(p, '#tk-register-form');
  }
  async function login(p, name, password) {
    await go(p, '/Account/Login');
    await p.type('#login-user', name);
    await p.type('#login-pass', password);
    await submit(p, '#tk-login-form');
  }
  function call(p, method, url, body) {
    return p.evaluate((m, u, b) => new Promise(resolve => {
      const fn = { GET: 'get', POST: 'post', PUT: 'put', DELETE: 'del' }[m];
      window.TK.api[fn](u, b === null ? undefined : b, { silent: true, allowAnonymous: true })
        .done(d => resolve({ ok: true, data: d === undefined ? null : d }))
        .fail(x => resolve({ ok: false, status: x && x.status, text: x && x.responseText ? String(x.responseText).slice(0, 400) : null }));
    }), method, url, body === undefined ? null : body);
  }
  const items = d => (Array.isArray(d) ? d : (d && (d.items || d.Items)) || []);
  const errorText = p => p.$eval('#tk-login-error span', e => e.textContent).catch(() => '');

  try {
    // 1. Oturumsuz sayfalar --------------------------------------------------------------------------------------------
    const anon = await fresh();
    await go(anon, '/');
    check('oturumsuz / → Giriş', anon.url().includes('/Account/Login'), anon.url());
    await go(anon, '/Kurulum');
    check('/Kurulum → Kayıt ol', anon.url().includes('/Account/Register'), anon.url());
    await go(anon, '/Account/Login');
    check('Giriş: "Hesabın yok mu? Kayıt ol" bağlantısı', await anon.$('#tk-to-register') !== null);
    check('Giriş etiketi e-posta da diyor', (await anon.$eval('label[for=login-user]', e => e.textContent)).includes('e-posta'));
    await setTheme(anon, 'light'); await shot(anon, 'giris-acik');
    await setTheme(anon, 'dark'); await shot(anon, 'giris-koyu');
    await go(anon, '/Account/Register');
    check('Kayıt: "Zaten hesabın var mı? Giriş yap" bağlantısı', await anon.$('#tk-to-login') !== null);
    await setTheme(anon, 'light'); await shot(anon, 'kayit-acik');
    await setTheme(anon, 'dark'); await shot(anon, 'kayit-koyu');
    await anon.type('#register-user', 'ab');
    await anon.type('#register-pass', 'kisa');
    await anon.click('#tk-register-form button[type=submit]');
    await sleep(400);
    check('Kayıt: kısa değerlerde form içi hata', await anon.$eval('#tk-register-error', e => !e.hidden && e.textContent.trim().length > 0));
    await setTheme(anon, 'light');

    const mobile = await fresh(390, 844);
    await go(mobile, '/Account/Login'); await shot(mobile, 'giris-390');
    await go(mobile, '/Account/Register'); await shot(mobile, 'kayit-390');
    await setTheme(mobile, 'dark'); await shot(mobile, 'kayit-390-koyu');
    const overflow = await mobile.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
    check('390 px: yatay taşma yok', overflow <= 1, overflow);

    // 2. A kayıt → Başlangıç → profil → çıkış → giriş ------------------------------------------------------------------
    const a = await fresh();
    await register(a, userA);
    check('A kayıt → Başlangıç', a.url().includes('/Baslangic'), a.url());
    await shot(a, 'a-baslangic');
    const profileName = 'A özel profil ' + stamp;
    const created = await call(a, 'POST', '/Profil/Ekle', { name: profileName, language: 'tr', format: 'Vertical', targetDurationSec: 30 });
    check('A profil ekledi', created.ok, created.ok ? undefined : created);
    const aProfiles = await call(a, 'GET', '/Profil/Liste');
    const aProfile = items(aProfiles.data).find(x => x.name === profileName);
    check('A profilini listede görüyor', !!aProfile);
    const out = await call(a, 'POST', '/Account/Logout', {});
    check('A çıkış yaptı', out.ok);
    await go(a, '/Baslangic');
    check('çıkıştan sonra Başlangıç → Giriş', a.url().includes('/Account/Login'), a.url());
    await login(a, userA, pass);
    check('A tekrar giriş yaptı', !a.url().includes('/Account/'), a.url());

    // 3. B kayıt → A'nın verisini göremez -------------------------------------------------------------------------------
    const b = await fresh();
    await register(b, userB);
    check('B kayıt → Başlangıç', b.url().includes('/Baslangic'), b.url());
    const bProfiles = await call(b, 'GET', '/Profil/Liste');
    check('B, A\'nın profilini listede görmüyor', bProfiles.ok && !items(bProfiles.data).some(x => x.name === profileName), items(bProfiles.data).map(x => x.name));
    if (aProfile) {
      const peek = await call(b, 'GET', '/Profil/Getir/' + encodeURIComponent(aProfile.id));
      check('B, A\'nın profilini kimlikle açamıyor (404)', !peek.ok && peek.status === 404, peek.status);
      const del = await call(b, 'DELETE', '/Profil/Sil/' + encodeURIComponent(aProfile.id));
      check('B, A\'nın profilini silemiyor', !del.ok, del.status);
      const still = await call(a, 'GET', '/Profil/Liste');
      check('A\'nın profili yerinde', items(still.data).some(x => x.name === profileName));
    }
    await go(b, '/Admin');
    check('B Yönetim paneline giremiyor', !b.url().includes('/Admin'), b.url());
    const bAdminApi = await call(b, 'GET', '/Admin/Dashboard/KayitAyari');
    check('B kayıt ayarını okuyamıyor', !bAdminApi.ok, bAdminApi.status);

    // 4. Yönetici: e-postayla giriş, kullanıcılar, kayıt anahtarı, devre dışı bırak ------------------------------------
    const admin = e2eAdmin();
    const ad = await fresh();
    await login(ad, admin.email, admin.password);
    check('Tohumlanan (sahte) yönetici e-postayla girdi', !ad.url().includes('/Account/'), ad.url());
    await go(ad, '/Admin#kullanicilar');
    await ad.waitForSelector('#ad-users-list table', { timeout: 15000 }).catch(() => null);
    await sleep(800);
    const rows = await ad.$$eval('#ad-users-list tbody tr', trs => trs.map(t => t.textContent));
    check('Yönetici listesinde A ve B var', rows.some(r => r.includes(userA)) && rows.some(r => r.includes(userB)));
    check('A ve B normal kullanıcı', rows.filter(r => r.includes(userA) || r.includes(userB)).every(r => !r.includes('Yönetici')));
    const toggleOn = await ad.$eval('#ad-registration-open', e => e.checked && !e.disabled);
    check('Kayıt anahtarı varsayılan açık', toggleOn);
    await setTheme(ad, 'light'); await shot(ad, 'admin-kullanicilar-acik');

    await ad.click('#ad-registration-open');
    await sleep(1200);
    const saved = await call(ad, 'GET', '/Admin/Dashboard/KayitAyari');
    check('Kayıt kapatıldı (sunucuda)', saved.ok && saved.data.registrationOpen === false, saved.data);
    await setTheme(ad, 'dark'); await shot(ad, 'admin-kullanicilar-koyu');

    const closed = await fresh();
    await go(closed, '/Account/Register');
    check('Kayıt kapalı: mesaj gösteriliyor', await closed.$('#tk-register-closed') !== null);
    check('Kayıt kapalı: form yok', await closed.$('#tk-register-form') === null);
    await shot(closed, 'kayit-kapali');
    const rejected = await call(closed, 'POST', '/Account/Register', { userName: 'e2e_c_' + stamp, password: pass, passwordConfirm: pass });
    check('Kayıt kapalı: Ajax kayıt 403', !rejected.ok && rejected.status === 403, rejected.status);

    await ad.click('#ad-registration-open');
    await sleep(1200);
    check('Kayıt tekrar açıldı', (await call(ad, 'GET', '/Admin/Dashboard/KayitAyari')).data.registrationOpen === true);

    const users = items((await call(ad, 'GET', '/Admin/Dashboard/Kullanicilar')).data);
    const bUser = users.find(u => u.userName === userB);
    const ban = await call(ad, 'POST', '/Admin/Dashboard/Yasakla/' + encodeURIComponent(bUser.id), {});
    check('B devre dışı bırakıldı', ban.ok, ban.status);
    const b2 = await fresh();
    await login(b2, userB, pass);
    check('Devre dışı B giriş yapamıyor', b2.url().includes('/Account/Login'), await errorText(b2));
    const unban = await call(ad, 'POST', '/Admin/Dashboard/YasagiKaldir/' + encodeURIComponent(bUser.id), {});
    check('B etkinleştirildi', unban.ok);
    await login(b2, userB, pass);
    check('Etkin B tekrar giriş yapıyor', !b2.url().includes('/Account/'), b2.url());
    const self = users.find(u => u.userName === admin.email);
    const selfBan = await call(ad, 'POST', '/Admin/Dashboard/Yasakla/' + encodeURIComponent(self.id), {});
    check('Yönetici kendini / son yöneticiyi devre dışı bırakamıyor', !selfBan.ok, selfBan.status);
  } finally {
    for (const c of contexts) { await c.close().catch(() => {}); }
  }
};
