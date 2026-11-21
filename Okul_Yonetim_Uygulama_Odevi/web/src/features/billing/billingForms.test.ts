import { centsText, moneyText, moneyTl, parseAmount, parsePercent, paymentBody, validatePaymentForm, validatePlanForm, validateTariffForm } from "@okul/api-client";

// Spec 0015 ortak istemci mantığı (birim): "1.234,50" ayrıştırma (kayan nokta yok, sunucuya "1234.50" metni), para metni, yüzde ve
// form sınırları (sunucuyla aynı).
describe("parseAmount", () => {
  it.each([
    ["1.234,50", "1234.50", 123450],
    ["1234,5", "1234.50", 123450],
    ["1234", "1234.00", 123400],
    ["12.50", "12.50", 1250],
    ["1.500", "1500.00", 150000],
    ["10.000.000,00", "10000000.00", 1_000_000_000],
    ["0,01", "0.01", 1],
    [" 7 000 ", "7000.00", 700000],
  ])("%s → %s", (input, text, cents) => {
    expect(parseAmount(input)).toEqual({ ok: true, text, cents });
  });

  it.each([
    ["", "required"],
    ["abc", "amountInvalid"],
    ["1,2,3", "amountInvalid"],
    ["0", "amountNotPositive"],
    ["-5", "amountNotPositive"],
    ["1,005", "amountDecimals"],
  ])("%s → %s", (input, error) => {
    expect(parseAmount(input)).toEqual({ ok: false, error });
  });
});

it("para metni kuruş tamsayısından; JSON sayısı ve metin aynı sonucu verir", () => {
  expect(moneyText(2833.33)).toBe("2.833,33");
  expect(moneyText("2833.33")).toBe("2.833,33");
  expect(moneyTl(10000000)).toBe("10.000.000,00 TL");
  expect(moneyText(0.1 + 0.2)).toBe("0,30");
  expect(centsText(-50)).toBe("-0,50");
});

it("yüzde 1–100 tam sayı", () => {
  expect(parsePercent("15")).toEqual({ ok: true, value: 15 });
  expect(parsePercent("100")).toEqual({ ok: true, value: 100 });
  for (const bad of ["0", "101", "12,5", "x"]) {
    expect(parsePercent(bad)).toEqual({ ok: false, error: "percentInvalid" });
  }
});

it("tarife formu: 10.000.000,00 kabul, 10.000.000,01 red", () => {
  expect(validateTariffForm({ name: "Eğitim", amount: "10.000.000,00" })).toEqual({});
  expect(validateTariffForm({ name: "", amount: "10.000.000,01" })).toEqual({ name: "required", amount: "amountTooLarge" });
});

it("plan formu: taksit 1–12, ilk vade bugün ile 365 gün sonrası", () => {
  const ok = { feeScheduleId: "f", discountId: "", installmentCount: "12", firstDueDate: "2027-11-20" };
  expect(validatePlanForm(ok, "2026-11-20")).toEqual({});
  expect(validatePlanForm({ ...ok, installmentCount: "0", firstDueDate: "2027-11-21" }, "2026-11-20")).toEqual({ installmentCount: "countInvalid", firstDueDate: "dueOutOfRange" });
  expect(validatePlanForm({ ...ok, feeScheduleId: "", installmentCount: "13", firstDueDate: "2026-11-19" }, "2026-11-20"))
    .toEqual({ feeScheduleId: "required", installmentCount: "countInvalid", firstDueDate: "dueOutOfRange" });
});

it("tahsilat formu: kalan borç kuruşla, tarih ve yöntem; gövde tutarı metin", () => {
  const values = { amount: "1.500,00", date: "2026-11-20", method: "Cash" as const, note: "" };
  expect(validatePaymentForm(values, "2026-11-20", 1500)).toEqual({});
  expect(validatePaymentForm({ ...values, amount: "1.500,01" }, "2026-11-20", 1500)).toEqual({ amount: "amountOverRemaining" });
  expect(validatePaymentForm({ ...values, date: "2025-11-19", method: "", note: "n".repeat(201) }, "2026-11-20", null))
    .toEqual({ date: "dateTooOld", method: "methodRequired", note: "tooLong" });
  expect(validatePaymentForm({ ...values, date: "2025-11-20" }, "2026-11-20", null)).toEqual({});
  expect(paymentBody("p", { ...values, note: "  dekont  " })).toEqual({ planId: "p", amount: "1500.00", date: "2026-11-20", method: "Cash", note: "dekont" });
});
