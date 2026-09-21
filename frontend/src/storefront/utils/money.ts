const SAR_LABEL = "ر.س";

export function formatStorefrontMoney(
  amount: number,
  currency: string,
) {
  const normalizedCurrency =
    currency.trim().toUpperCase();

  let formattedAmount: string;

  try {
    formattedAmount = new Intl.NumberFormat(
      "en-US",
      {
        minimumFractionDigits: 0,
        maximumFractionDigits:
          Number.isInteger(amount)
            ? 0
            : 2,
      },
    ).format(amount);
  }
  catch {
    formattedAmount = String(amount);
  }

  const currencyLabel =
    normalizedCurrency === "SAR"
      ? SAR_LABEL
      : normalizedCurrency || currency;

  return `${formattedAmount} ${currencyLabel}`;
}
