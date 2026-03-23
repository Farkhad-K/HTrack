# HTrack Bot — Foydalanish Qo'llanmasi

HTrack — RFID kartalari orqali xodimlarning kelish-ketishini kuzatuvchi tizim.
Menejerlar Telegram bot orqali barcha ma'lumotlarga kirishadi.

---

## Boshlash

Botni ishga tushirish uchun `/start` buyrug'ini yuboring.
Bot sizga pastki menyu tugmalarini ko'rsatadi va kompaniyangizga ruxsat borligini tekshiradi.

> Agar "Siz hech qanday kompaniyaga ruxsatga ega emassiz" xabari chiqsa — admin bilan bog'laning.

---

## Tugmalar menyusi

Botni ochganda pastda doimiy tugmalar paneli ko'rinadi:

| Tugma | Vazifasi |
|-------|----------|
| 👥 Xodimlar | Barcha xodimlar va RFID kodlari ro'yxati |
| ✅ Ishda | Hozir ishda bo'lgan xodimlar |
| 🚪 Ishdan chiqdi | Bugun ishni tugatgan xodimlar |
| 📊 O'tgan oy | O'tgan oy uchun kompaniya Excel hisoboti |
| 📆 Bugunga | Bitta xodimning oy boshidan bugungacha batafsil hisoboti |
| 📋 Xodim oylik | Bitta xodimning oy boshidan bugunga hisoboti |
| 📅 15 kunlik | Kompaniyadagi barcha xodimlarning joriy 15 kunlik batafsil hisoboti |
| 🗓 Ixtiyoriy sana | Bitta xodimning o'zingiz belgilagan sana hisoboti |
| ✏️ Davomat | Qo'lda davomat kiritish (RFID orqali) |
| 🔄 Yangilash | Xodim ismini RFID orqali yangilash |

> **Ko'p bosqichli amalni bekor qilish:** istalgan vaqt boshqa tugma bosingiz yoki `/cancel` yuboring — bot joriy amalni to'xtatadi va asosiy menyuga qaytadi.

---

## Buyruqlar

### `/cancel` — Amalni bekor qilish

Ko'p bosqichli buyruq davomida jarayonni to'xtatish uchun ishlating.

- Har qanday holatda ishlaydi — hech qanday amal kutilmayotgan bo'lsa ham xato bermaydi.
- Istalgan tugmani bosish ham xuddi shu ishni bajaradi.

---

### `/employees` — Xodimlar ro'yxati

Kompaniyangizdagi barcha xodimlarni va ularning RFID karta kodlarini ko'rsatadi.

```
👥 Xodimlar ro'yxati:
• Eshmat Toshmatov (RFID: ABCDEF01)
• Dilnoza Yusupova (RFID: 12345678)
```

---

### `/checked_in` — Hozir ishda

Hozirgi vaqtda ish joyida bo'lgan (check-in qilgan, lekin check-out qilmagan) xodimlar ro'yxati.

```
✅ Hozirda ishda bo'lgan xodimlar:
• Eshmat Toshmatov (RFID: ABCDEF01) at 08:45:12
```

---

### `/checked_out` — Bugun ketganlar

Bugun ish vaqtini yakunlagan xodimlar ro'yxati.

```
🏁 Bugun ishni tugatgan xodimlar:
• Dilnoza Yusupova (RFID: 12345678) at 17:30:05
```

---

### `/excel_report` — O'tgan oy kompaniya hisoboti

O'tgan kalendar oyi uchun kompaniyadagi barcha xodimlarning Excel fayli yuboriladi.

Fayl ikki varaqdan iborat:
- **Xulosa** — har bir xodim uchun ish kunlari soni, jami soat, o'rtacha soat/kun, anomaliya soni, qo'lda yozuvlar soni
- **Batafsil** — har bir kelish-ketish yozuvi (sana, vaqt, davomiylik, holat, manba)

---

### `/report_till_today` — Xodimning oy boshidan bugungacha hisoboti

Joriy oyning 1-kunidan bugungi sanagacha **bitta xodim** davomatini Excel formatida beradi.

**Qadamlar:**

1. `/report_till_today` yuboring yoki `📆 Bugunga` tugmasini bosing
2. Bot RFID kodni so'raydi:
   ```
   📆 Xodimning RFID kodini kiriting:
   ```
3. RFID kodni yuboring
4. Bot xodimning shaxsiy Excel faylini yuboradi.

---

### `/company_report_till_today` — Oy boshidan bugunga kompaniya hisoboti

Joriy oyning 1-kunidan bugungi sanagacha bo'lgan barcha xodimlar davomatini o'z ichiga oladi.

---

### `/15daysreport` — Kompaniyaning 15 kunlik hisoboti

Joriy oyning birinchi yoki ikkinchi yarmini avtomatik aniqlaydi va **kompaniyadagi barcha xodimlar** uchun batafsil hisobot beradi.

**Qadamlar:**

1. `/15daysreport` yuboring yoki `📅 15 kunlik` tugmasini bosing
2. Bot darhol Excel faylni yuboradi.
3. Faylda barcha xodimlarning batafsil davomat yozuvlari bo'ladi: qaysi kuni, nechida kelgani, nechida ketgani va qancha ishlagani.

**Qaysi davr hisoblanadi?**
- Oy 1–15 kunlari ichida bo'lsangiz → 1-15-kun oralig'i
- Oy 16-kunidan keyin bo'lsangiz → 16-oy oxiri oralig'i

---

### `/employee_monthly` — Xodimning oylik hisoboti

Joriy oyning 1-kunidan bugungi sanagacha bitta xodim uchun hisobot.

**Qadamlar:**

1. `/employee_monthly` yuboring yoki `📋 Xodim oylik` tugmasini bosing
2. Bot RFID kodni so'raydi:
   ```
   📋 Xodimning RFID kodini kiriting:
   ```
3. RFID kodni yuboring
4. Bot xodimning shaxsiy Excel faylini yuboradi.

---

### `/custom_report` — Xodimning ixtiyoriy sana hisoboti

Siz belgilagan ikki sana orasidagi **bitta xodim** davomatini Excel formatida beradi.

**Qadamlar:**

1. `/custom_report` yuboring yoki `🗓 Ixtiyoriy sana` tugmasini bosing
2. Bot RFID kodni so'raydi:
   ```
   🗓 Xodimning RFID kodini kiriting:
   ```
3. RFID kodni yuboring:
   ```
   ABCDEF01
   ```
4. Bot boshlanish sanasini so'raydi:
   ```
   🗓 Boshlanish sanasini kiriting (format: dd.MM.yyyy)
   Misol: 01.01.2025
   ```
5. Sanani kiriting:
   ```
   15.02.2025
   ```
6. Bot tugash sanasini so'raydi:
   ```
   🗓 Tugash sanasini kiriting (format: dd.MM.yyyy)
   Misol: 31.01.2025
   ```
7. Tugash sanasini kiriting:
   ```
   28.02.2025
   ```
8. Bot Excel faylni yuboradi.

> **Format qat'iy:** `kun.oy.yil` — masalan `05.03.2025`. Boshqa format ishlamaydi.

---

### `/new_attendance` — Qo'lda davomat kiritish

RFID skaner ishlamagan yoki xodim kartasiz kelgan holatlarda qo'lda check-in/check-out qilish uchun.

**Qadamlar:**

1. `/new_attendance` yuboring yoki `✏️ Davomat` tugmasini bosing
2. Bot RFID kodni so'raydi:
   ```
   📮 Iltimos, xodimning RFID UID kodini yuboring.
   ```
3. RFID kodni yuboring (bo'sh joy bilan yoki joylashtirib):
   ```
   AB CD EF 01
   ```
4. Tizim avtomatik ravishda:
   - Xodim hali check-in qilmagan bo'lsa → **kelish vaqti** belgilanadi
   - Xodim allaqachon check-in qilgan bo'lsa → **ketish vaqti** belgilanadi va davomiylik hisoblanadi

> **Takroriy scan himoyasi:** bir xil RFID juda qisqa vaqt ichida qayta yuborilsa, tizim uni takroriy scan deb e'tiborsiz qoldirishi mumkin. Bu tasodifiy ikki marta bosish oqibatida noto'g'ri kelish/ketish yozuvi paydo bo'lishining oldini oladi.

---

### `/update_employee` — Xodim ismini yangilash

Xodimning ismi o'zgarganda RFID kodi orqali yangilash.

**Qadamlar:**

1. `/update_employee` yuboring yoki `🔄 Yangilash` tugmasini bosing
2. Bot RFID va yangi ismni so'raydi:
   ```
   ✏️ Iltimos, xodimning RFID UID va to'liq ismini vergul bilan ajratib yuboring.

   Misol:
   00 00 00 00, Eshmat Toshmatov
   ```
3. Ma'lumotni yuboring:
   ```
   AB CD EF 01, Jasur Karimov
   ```
4. Tizim ismni yangilaydi va tasdiqlaydi.

---

## Excel hisobot ranglari (Batafsil varaqi)

| Rang | Ma'nosi |
|------|---------|
| 🟢 Yashil fon | Xodim 8 soat va undan ko'p ishlagan |
| 🔴 Qizil fon | Xodim 4 soatdan kam ishlagan |
| Rangsiz | 4–8 soat oralig'ida ishlagan yoki hali ishdan chiqmagan |
| 🟧 To'q sariq — "Holat" katagi | Shubhali yozuv: uzoq smena yoki vaqt/davomiylik nomuvofiqligi |
| 🟦 Ko'kimtir — "Manba" katagi | Qo'lda kelish yoki qo'lda ketish mavjud |
| 🟩 To'q yashil — birinchi qator | Kompaniya hisobotida har bir xodimning ajratuvchi qatori |
| 🟨 Och sariq — "Kun jami" qatori | Xodim hisobotida har kunning yig'indi soati |
| 🟡 Sariq — "Jami" qatori | Butun davr uchun umumiy jami ishlagan soat |

---

## Xodim hisobot varaqlarining tuzilishi

**Xulosa varaqi** — bir qator: xodim ismi, ish kunlari soni, jami soat, o'rtacha soat/kun, anomaliya soni, qo'lda yozuvlar soni.

Agar qo'lda yozuvlar bo'lsa, shu varaqning pastida **"Qo'lda yozuvlar"** bo'limi ham chiqadi.

**Batafsil varaqi** — har bir kelish-ketish alohida qatorda, kunlar bo'yicha guruhlangan:

| Kelish | Ketish | Smena soati | Kun jami | Holat | Manba |
|--------|--------|-------------|----------|-------|-------|
| 14.03.2025 08:45 | 14.03.2025 17:30 | 08:45 | | | Qo'lda kelish |
| 14.03.2025 — Kun jami | | | 08:45 | | |
| 15.03.2025 09:00 | — | 00:00 | | | Qo'lda kelish |
| 15.03.2025 — Kun jami | | | 00:00 | | |
| Jami | | | 08:45 | | |

- Har kunning oxirida **"Kun jami"** qatori (och sariq fonda) ko'rsatiladi
- Eng oxirgi **"Jami"** qatori (sariq fonda) — butun davr yig'indisi
- **"Holat"** ustuni shubhali yozuvlarni ko'rsatadi, lekin soatlar hisobini to'xtatmaydi
- **"Manba"** ustuni qo'lda kiritilgan kelish/ketishlarni ko'rsatadi
- Tungi smena uchun: kelish va ketish sanasi-vaqti to'liq ko'rsatiladi (masalan `31.03.2025 22:00` → `01.04.2025 06:00`)

> **"Ketish" bo'sh (`—`) bo'lsa** — xodim hali ishdan chiqmagan yoki check-out qilinmagan. Rang ko'rsatilmaydi.

---

## Tungi smenalar va oy chegarasi

**Muhim:** Hisobot filtri har doim **kelish vaqti (CheckIn)** asosida ishlaydi.

**Misol:** Xodim 31-mart kuni soat 22:00 da keldi va 1-aprel soat 06:00 da ketdi (8 soat ishladi).
- Bu yozuv **mart oyining** hisobotida ko'rinadi — aprelda emas.
- Davomiylik to'liq **8 soat** ko'rsatiladi, chunki ketish vaqti boshqa oyda bo'lsa ham, davomiylik (Duration) bazada to'liq saqlanadi.

**Sababi:** Smenani "qaysi oyga tegishli" deb belgilashda kelish vaqti mantiqliyroq va kutilgan — agar ishchi kechqurun kirsa, u shu kunning smenasidir.

---

## Tez-tez so'raladigan savollar

**Savol:** RFID kodi qaysi formatda kiritiladi?
**Javob:** Bo'sh joy bilan ajratilgan yoki bitishmagan hex baytlar — masalan `AB CD EF 01` yoki `ABCDEF01`. Tizim avtomatik ravishda bo'shliqlarni olib tashlaydi va katta harflarga o'tkazadi.

**Savol:** Sana noto'g'ri formatda kiritsam nima bo'ladi?
**Javob:** Bot xato xabarini ko'rsatadi va yana kiritish imkonini beradi. 3 marta ketma-ket xato kiritilsa, jarayon avtomatik bekor qilinadi — buyruqni qaytadan boshlang.

**Savol:** Ko'p bosqichli amaldan qanday chiqish mumkin?
**Javob:** `/cancel` yuboring yoki istalgan tugmani bosing. Bot "Amal bekor qilindi" deb asosiy menyuga qaytaradi.

**Savol:** Hisobot bo'sh kelsa?
**Javob:** Tanlangan sana oralig'ida hech qanday davomat yozuvi yo'q. RFID skaneri ishlaganini tekshiring yoki sana oralig'ini kengaytiring.

**Savol:** Bir xodim bir kunda bir necha marta chiqib-kirsa?
**Javob:** Har bir juft (kelish–ketish) alohida qatorda ko'rsatiladi. Kunning barcha smenalari tugagach, "Kun jami" qatori o'sha kunning umumiy soatini ko'rsatadi.

**Savol:** Qo'lda kiritilgan davomatni reportdan qanday bilaman?
**Javob:** Excel faylda `Manba` ustunida `Qo'lda kelish`, `Qo'lda ketish` yoki `Qo'lda kelish/ketish` deb ko'rsatiladi. `Xulosa` varag'ida esa qo'lda yozuvlar soni va alohida qisqa ro'yxat chiqadi.

**Savol:** Nega ba'zi scanlar darhol yangi kelish/ketish qilib qo'shilmaydi?
**Javob:** Tizim juda qisqa vaqt ichidagi takroriy RFID scanlarni e'tiborsiz qoldiradi. Bu tasodifiy ikki marta bosish sababli noto'g'ri attendance yozuvi hosil bo'lishining oldini oladi.

**Savol:** 15 kunlik hisobot nima uchun ba'zan boshqa sanalarni ko'rsatadi?
**Javob:** Joriy oy 1–15-kuni ichida bo'lsangiz 1–15, 16-kunidan keyin bo'lsangiz 16–oy oxiri ko'rsatiladi. Bu avtomatik aniqlanadi.

**Savol:** Kompaniya hisoboti bilan xodim hisoboti farqi nima?
**Javob:** `📊 O'tgan oy`, `📅 15 kunlik` va `/company_report_till_today` — kompaniyadagi **barcha** xodimlarni o'z ichiga oladi. `📆 Bugunga`, `📋 Xodim oylik`, `🗓 Ixtiyoriy sana` va `/report_till_today` — faqat siz ko'rsatgan RFID kodi bo'yicha **bitta xodim** uchun hisobot beradi.

**Savol:** Tungi smena boshqa oyga o'tib ketsa, qaysi oyda hisoblanadi?
**Javob:** Kelish vaqti (CheckIn) qaysi oyda bo'lsa, shu oyning hisobotida ko'rinadi. Masalan, mart kechqurun kirgan xodim mart hisobotida bo'ladi, hatto aprel tongida ketsa ham. Ishlagan soatlar to'liq hisobga olinadi.
