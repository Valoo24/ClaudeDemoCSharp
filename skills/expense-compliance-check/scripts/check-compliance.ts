import { readFileSync } from "node:fs";
import { join } from "node:path";

// Expects exactly two arguments: the expense category and the EUR amount.
const args = process.argv.slice(2);
if (args.length !== 2) {
  console.error("Usage: node scripts/check-compliance.ts <category> <amount>");
  process.exit(1);
}

const [categoryText, amountText] = args;
if (!/^-?\d+(\.\d+)?$/.test(amountText)) {
  console.error(`Invalid amount: ${amountText}`);
  process.exit(1);
}

const amount = Number(amountText);

const validCategories = ["meals", "transport", "lodging", "equipment", "other"];
const lowered = categoryText.toLowerCase();
const category = validCategories.includes(lowered) ? lowered : "other";

const caps: { caps: Record<string, number | null> } = JSON.parse(
  readFileSync(join(import.meta.dirname, "caps.json"), "utf-8"),
);

// "transport" has no cap in the policy table (null in caps.json).
const cap = caps.caps[category];

let verdict: string;
if (cap === null || amount <= cap) {
  verdict = "APPROVED";
} else if (category === "equipment") {
  // Equipment is the one category where exceeding the cap does not
  // reject the expense outright, it just requires manager sign-off.
  verdict = "NEEDS_MANAGER_APPROVAL";
} else {
  verdict = "OVER_CAP";
}

// The amount is written as given (184.00 stays 184.00): JSON.stringify would
// drop the trailing zeros.
console.log(
  `{"category":${JSON.stringify(category)},"cap":${JSON.stringify(cap === null ? "none" : String(cap))},"amount":${amountText},"verdict":${JSON.stringify(verdict)}}`,
);
