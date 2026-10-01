---
name: currency-conversion
description: Converts an amount from a foreign currency into EUR using fixed indicative exchange rates. Use this whenever an amount needs to be expressed in EUR before further processing, such as before a compliance check.
---

# Currency Conversion

This skill converts an amount from a supported foreign currency into EUR using
a fixed table of indicative exchange rates. It is deliberately narrow: it only
converts. It does not judge whether an amount complies with any policy - see
the compliance-check skill for that, and pass it the converted EUR amount.

For worked examples, see references/examples.md.

## Supported currencies (fixed indicative rates, base EUR)

| Currency | Rate to EUR |
|----------|-------------|
| USD      | 0.92        |
| GBP      | 1.17        |
| CHF      | 1.04        |

## Steps

1. Identify the amount and the source currency code from the request.
2. If the currency is already EUR, report the amount unchanged - do not run
   the script, there is nothing to convert.
3. Otherwise, run:
   node scripts/convert-currency.ts <amount> <currency>
   Use the printed value as-is - do not estimate or recompute the conversion
   yourself.
4. Report the result in this exact form:
   `<amount> <currency> = <converted> EUR`
5. If the currency code is not in the table above, say so plainly and ask for
   a supported code, or ask the user to provide the EUR amount directly.

## Notes

- Always run the script - never estimate a conversion.
- These are fixed, indicative rates, not live market rates - say so if asked
  for precision or a live rate.
- This skill never decides compliance or category - it only converts.
