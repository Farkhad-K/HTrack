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
| 📊 O'tgan oy | O'tgan oy uchun Excel hisoboti |
| 📅 15 kunlik | Joriy oyning 15 kunlik hisoboti |
| 📆 Bugunga | Oy boshidan bugunga qadar hisobot |
| 🗓 Ixtiyoriy sana | O'zingiz belgilagan sana oralig'i hisoboti |
| ✏️ Davomat | Qo'lda davomat kiritish (RFID orqali) |
| 🔄 Yangilash | Xodim ismini RFID orqali yangilash |

> **Ko'p bosqichli amalni bekor qilish:** istalgan vaqt boshqa tugma bosingiz yoki `/cancel` yuboring — bot joriy amalni to'xtatadi va asosiy menyuga qaytadi.

---

## Buyruqlar

### `/cancel` — Amalni bekor qilish

Ko'p bosqichli buyruq davomida (`/custom_report`, `/new_attendance`, `/update_employee`) jarayonni to'xtatish uchun ishlating.

- Har qanday holatda ishlaydi — hech qanday amal kutilmayotgan bo'lsa ham xato bermaydi.
- Istalgan tugmani bosish ham xuddi shu ishni bajaradi.

---

### `/employees` — Xodimlar ro'yxati
Kompaniyangizdagi barcha xodimlarni va ularning RFID karta kodlarini ko'rsatadi.

```
👥 Xodimlar ro'yxati:
• Eshmat Toshmatov (RFID: AB CD EF 01)
• Dilnoza Yusupova (RFID: 12 34 56 78)
```

---

### `/checked_in` — Hozir ishda
Hozirgi vaqtda ish joyida bo'lgan (check-in qilgan, lekin check-out qilmagan) xodimlar ro'yxati.

```
✅ Hozirda ishda bo'lgan xodimlar:
• Eshmat Toshmatov (RFID: AB CD EF 01) at 08:45:12
```

---

### `/checked_out` — Bugun ketganlar
Bugun ish vaqtini yakunlagan xodimlar ro'yxati.

```
🏁 Bugun ishni tugatgan xodimlar:
• Dilnoza Yusupova (RFID: 12 34 56 78) at 17:30:05
```

---

### `/excel_report` — O'tgan oy hisoboti
O'tgan kalendar oyi uchun to'liq Excel fayli yuboriladi.

Fayl ikki varaqdan iborat:
- **Xulosa** — har bir xodim uchun ish kunlari soni, jami soat, o'rtacha soat/kun
- **Batafsil** — har bir kelish-ketish yozuvi (vaqt, davomiylik)

---

### `/15daysreport` — 15 kunlik hisobot
Joriy oyning birinchi yoki ikkinchi yarmini avtomatik aniqlaydi:
- Oy 1–15 kunlari ichida bo'lsangiz → 1-15-kun oralig'i
- Oy 16-kunidan keyin bo'lsangiz → 16-oy oxiri oralig'i

---

### `/report_till_today` — Oy boshidan bugunga
Joriy oyning 1-kunidan bugungi sanagacha bo'lgan barcha davomatni o'z ichiga oladi.

---

### `/custom_report` — Ixtiyoriy sana oralig'i hisoboti

Siz belgilagan ikki sana orasidagi davomatni Excel formatida beradi.

**Qadamlar:**

1. `/custom_report` yuboring yoki `🗓 Ixtiyoriy sana` tugmasini bosing
2. Bot boshlanish sanasini so'raydi:
   ```
   🗓 Boshlanish sanasini kiriting (format: dd.MM.yyyy)
   Misol: 01.01.2025
   ```
3. Sanani kiriting, masalan:
   ```
   15.02.2025
   ```
4. Bot tugash sanasini so'raydi:
   ```
   📅 Tugash sanasini kiriting (format: dd.MM.yyyy)
   Misol: 31.01.2025
   ```
5. Tugash sanasini kiriting:
   ```
   28.02.2025
   ```
6. Bot Excel faylni yuboradi.

> **Format qat'iy:** `kun.oy.yil` — masalan `05.03.2025`. Boshqa format ishlmaydi.

---

### `/new_attendance` — Qo'lda davomat kiritish

RFID skaner ishlamagan yoki xodim kartasiz kelgan holatlarda qo'lda check-in/check-out qilish uchun.

**Qadamlar:**

1. `/new_attendance` yuboring yoki `✏️ Davomat` tugmasini bosing
2. Bot RFID kodni so'raydi:
   ```
   📮 Iltimos, xodimning RFID UID kodini yuboring.
   ```
3. RFID kodni yuboring (bo'sh joy bilan ajratilgan hex formatida):
   ```
   AB CD EF 01
   ```
4. Tizim avtomatik ravishda:
   - Xodim hali check-in qilmagan bo'lsa → **kelish vaqti** belgilanadi
   - Xodim allaqachon check-in qilgan bo'lsa → **ketish vaqti** belgilanadi va davomiylik hisoblanadi

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
| Rangsiz | 4–8 soat oralig'ida ishlagan |
| 🟡 Sariq — "Jami" qatori | Har bir xodim uchun jami ishlagan soat |

---

## Tez-tez so'raladigan savollar

**Savol:** RFID kodi qaysi formatda kiritiladi?
**Javob:** Bo'sh joy bilan ajratilgan hex baytlar — masalan `AB CD EF 01` yoki `00 1A 2B 3C`.

**Savol:** Sana noto'g'ri formatda kiritsam nima bo'ladi?
**Javob:** Bot xato xabarini ko'rsatadi va yana kiritish imkonini beradi. 3 marta ketma-ket xato kiritilsa, jarayon avtomatik bekor qilinadi — buyruqni qaytadan boshlang.

**Savol:** Ko'p bosqichli amaldan qanday chiqish mumkin?
**Javob:** `/cancel` yuboring yoki istalgan tugmani bosing. Bot "Amal bekor qilindi" deb asosiy menyuga qaytaradi.

**Savol:** Hisobot bo'sh kelsa?
**Javob:** Tanlangan sana oralig'ida hech qanday davomat yozuvi yo'q. RFID skaneri ishlaganini tekshiring.

**Savol:** Bir xodim bir kunda bir necha marta chiqib-kirsa?
**Javob:** Har bir juft (kelish–ketish) alohida qatorda ko'rsatiladi. Jami soat barcha yozuvlar yig'indisi.
