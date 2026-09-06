# OFOQ Payment API — NEW FILES ONLY

هذه الحزمة تحتوي فقط على ملفات C# الجديدة لمرحلة Payment API.
لا تحتوي على نسخة كاملة من المشروع، ولا تحتوي على ملفات C# قديمة للاستبدال.

## طريقة النسخ

انسخ مجلدي `src` و `tests` فوق جذر المشروع:

`C:\OFOQ.Market1`

مع الحفاظ على نفس المسارات.

> إذا طلب Windows استبدال أي ملف C# من هذه الحزمة، توقف وتحقق من المسار؛ ملفات C# داخل الحزمة يفترض أنها جديدة.

بعد النسخ افتح:

`MANUAL_CHANGES_EXISTING_FILES.md`

ونفذ التعديلات الأربعة الصغيرة على الملفات الموجودة أصلًا.

## ما الذي تضيفه المرحلة؟

- طرق الدفع المتاحة حسب Order نفسه.
- إنشاء PaymentIntent بمبلغ وعملة مأخوذين من Order فقط.
- Idempotency-Key لإنشاء PaymentIntent.
- قراءة PaymentIntent المملوك للعميل.
- Retry ينشئ PaymentIntent جديدًا لنفس Payment والطريقة فقط عند Failed/Cancelled/Expired.
- منع CashOnDelivery من Payment API حاليًا مع بقائه placeholder في enum.
- احترام تعليق Electronic Payments على مستوى Tenant.
- PostgreSQL transaction/advisory locking لمنع سباقات إنشاء Payment/Intent.
- Application/API/Integration tests جديدة.

## لا يوجد في هذه المرحلة

- Sham Cash provider implementation
- Syriatel Cash provider implementation
- Webhooks
- Store Wallet
- Manual receipt workflow
- Refunds
- Order -> Paid transition
- Migration جديدة
