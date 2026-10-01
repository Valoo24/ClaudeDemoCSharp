import { readFileSync } from "node:fs";
import { join } from "node:path";

// Expects exactly two arguments: the amount and the source currency code.
const args = process.argv.slice(2);
if (args.length !== 2) {
  console.error("Usage: node scripts/convert-currency.ts <amount> <currency>");
  process.exit(1);
}

const [amountText, currencyText] = args;
if (!/^-?\d+(\.\d+)?$/.test(amountText)) {
  console.error(`Invalid amount: ${amountText}`);
  process.exit(1);
}

const amount = Number(amountText);
const currency = currencyText.toUpperCase();

// EUR is the base currency used for the reimbursement caps, no conversion needed.
if (currency === "EUR") {
  console.log(amountText);
  process.exit(0);
}

const rates: { rates: Record<string, number> } = JSON.parse(
  readFileSync(join(import.meta.dirname, "rates.json"), "utf-8"),
);

const rate = rates.rates[currency];
if (rate === undefined) {
  console.error(`Unsupported currency: ${currency}`);
  process.exit(1);
}

// Always two decimals (165.60, not 165.6), like the amounts in the examples.
console.log((Math.round(amount * rate * 100) / 100).toFixed(2));
