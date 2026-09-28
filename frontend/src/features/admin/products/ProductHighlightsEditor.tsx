import {
  useEffect,
  useState,
} from "react";

import {
  getProductContentBlocks,
  setProductContentBlocks,
  type ProductContentBlock,
  type ProductContentBlockInput,
} from "./productsApi";

interface ProductHighlightsEditorProps {
  tenantId: string | null;
  productId: string;
}

interface HighlightDraft {
  id: string;
  title: string;
  body: string;
  isVisible: boolean;
}

function makeId() {
  return crypto.randomUUID();
}

export function ProductHighlightsEditor({
  tenantId,
  productId,
}: ProductHighlightsEditorProps) {
  const requestKey =
    tenantId && productId
      ? `${tenantId}:${productId}`
      : null;

  const [loadedKey, setLoadedKey] =
    useState<string | null>(null);

  const [otherBlocks, setOtherBlocks] =
    useState<ProductContentBlock[]>([]);

  const [items, setItems] =
    useState<HighlightDraft[]>([]);

  const [saving, setSaving] =
    useState(false);

  const [error, setError] =
    useState("");

  const [notice, setNotice] =
    useState("");

  const loading =
    Boolean(
      requestKey &&
      loadedKey !== requestKey,
    );

  useEffect(() => {
    if (
      !tenantId ||
      !productId ||
      !requestKey
    ) {
      return;
    }

    let cancelled = false;

    void getProductContentBlocks(
      tenantId,
      productId,
    )
      .then((response) => {
        if (cancelled) {
          return;
        }

        const highlights =
          response.blocks
            .filter(
              (block) =>
                block.type
                  .trim()
                  .toLowerCase() ===
                "highlight",
            )
            .sort(
              (a, b) =>
                a.sortOrder -
                b.sortOrder,
            )
            .slice(0, 6)
            .map((block) => ({
              id: block.blockId,
              title:
                block.title ?? "",
              body:
                block.body ?? "",
              isVisible:
                block.isVisible,
            }));

        const preserved =
          response.blocks.filter(
            (block) =>
              block.type
                .trim()
                .toLowerCase() !==
              "highlight",
          );

        setItems(
          highlights,
        );

        setOtherBlocks(
          preserved,
        );
      })
      .catch((caught) => {
        if (cancelled) {
          return;
        }

        setError(
          caught instanceof Error
            ? caught.message
            : "تعذر تحميل مميزات المنتج.",
        );
      })
      .finally(() => {
        if (!cancelled) {
          setLoadedKey(
            requestKey,
          );
        }
      });

    return () => {
      cancelled = true;
    };
  }, [
    tenantId,
    productId,
    requestKey,
  ]);

  function add() {
    if (
      items.length >= 6
    ) {
      return;
    }

    setItems(
      (current) => [
        ...current,
        {
          id: makeId(),
          title: "",
          body: "",
          isVisible: true,
        },
      ],
    );

    setError("");
    setNotice("");
  }

  function update(
    id: string,
    patch: Partial<HighlightDraft>,
  ) {
    setItems(
      (current) =>
        current.map(
          (item) =>
            item.id === id
              ? {
                  ...item,
                  ...patch,
                }
              : item,
        ),
    );

    setNotice("");
  }

  function remove(
    id: string,
  ) {
    setItems(
      (current) =>
        current.filter(
          (item) =>
            item.id !== id,
        ),
    );

    setNotice("");
  }

  function move(
    index: number,
    direction: -1 | 1,
  ) {
    const nextIndex =
      index + direction;

    if (
      nextIndex < 0 ||
      nextIndex >= items.length
    ) {
      return;
    }

    setItems(
      (current) => {
        const next =
          [...current];

        const [item] =
          next.splice(
            index,
            1,
          );

        next.splice(
          nextIndex,
          0,
          item,
        );

        return next;
      },
    );

    setNotice("");
  }

  async function save() {
    if (
      !tenantId ||
      !productId ||
      saving ||
      loading
    ) {
      return;
    }

    setError("");
    setNotice("");

    const normalized =
      items.map(
        (item) => ({
          ...item,
          title:
            item.title.trim(),
          body:
            item.body.trim(),
        }),
      );

    const invalid =
      normalized.find(
        (item) =>
          !item.title ||
          item.title.length > 80 ||
          item.body.length > 220,
      );

    if (invalid) {
      setError(
        "كل ميزة تحتاج عنوانًا، بحد أقصى 80 حرفًا، والوصف بحد أقصى 220 حرفًا.",
      );

      return;
    }

    if (
      otherBlocks.length +
        normalized.length >
      20
    ) {
      setError(
        "وصل المنتج إلى الحد الأقصى للمحتوى الإضافي.",
      );

      return;
    }

    const preserved:
      ProductContentBlockInput[] =
      [...otherBlocks]
        .sort(
          (a, b) =>
            a.sortOrder -
            b.sortOrder,
        )
        .map(
          (block) => ({
            type:
              block.type,
            title:
              block.title,
            body:
              block.body,
            mediaUrl:
              block.mediaUrl,
            isVisible:
              block.isVisible,
          }),
        );

    const highlights:
      ProductContentBlockInput[] =
      normalized.map(
        (item) => ({
          type:
            "Highlight",
          title:
            item.title,
          body:
            item.body || null,
          mediaUrl:
            null,
          isVisible:
            item.isVisible,
        }),
      );

    setSaving(true);

    try {
      const response =
        await setProductContentBlocks(
          tenantId,
          productId,
          [
            ...preserved,
            ...highlights,
          ],
        );

      setOtherBlocks(
        response.blocks.filter(
          (block) =>
            block.type
              .trim()
              .toLowerCase() !==
            "highlight",
        ),
      );

      setItems(
        response.blocks
          .filter(
            (block) =>
              block.type
                .trim()
                .toLowerCase() ===
              "highlight",
          )
          .sort(
            (a, b) =>
              a.sortOrder -
              b.sortOrder,
          )
          .slice(0, 6)
          .map(
            (block) => ({
              id:
                block.blockId,
              title:
                block.title ?? "",
              body:
                block.body ?? "",
              isVisible:
                block.isVisible,
            }),
          ),
      );

      setNotice(
        "تم حفظ مميزات المنتج.",
      );
    }
    catch (caught) {
      setError(
        caught instanceof Error
          ? caught.message
          : "تعذر حفظ مميزات المنتج.",
      );
    }
    finally {
      setSaving(false);
    }
  }

  return (
    <section className="rounded-[14px] border border-black/[0.07] bg-[#fafaf8] p-5">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <p className="text-[12px] font-semibold">
            مميزات المنتج
          </p>

          <p className="mt-1 max-w-xl text-[9px] leading-5 text-black/45">
            معلومات قصيرة تظهر للعميل أسفل أزرار الشراء.
            يمكنك تعديلها أو إخفاؤها أو حذفها بالكامل.
          </p>
        </div>

        <button
          type="button"
          onClick={add}
          disabled={
            loading ||
            saving ||
            items.length >= 6
          }
          className="rounded-[9px] border border-black/10 bg-white px-4 py-2 text-[10px] font-semibold disabled:opacity-40"
        >
          + إضافة ميزة
        </button>
      </div>

      {loading ? (
        <p className="mt-5 text-[10px] text-black/45">
          جاري تحميل المميزات...
        </p>
      ) : null}

      {!loading &&
      items.length === 0 ? (
        <div className="mt-5 rounded-[10px] border border-dashed border-black/10 bg-white px-4 py-5 text-center text-[10px] text-black/45">
          لا توجد مميزات مضافة.
          لن يظهر هذا القسم للعميل.
        </div>
      ) : null}

      <div className="mt-4 space-y-3">
        {items.map(
          (
            item,
            index,
          ) => (
            <div
              key={item.id}
              className="rounded-[11px] border border-black/[0.08] bg-white p-4"
            >
              <div className="grid gap-3 md:grid-cols-[1fr_1.5fr]">
                <label>
                  <span className="mb-1.5 block text-[9px] font-semibold text-black/55">
                    العنوان
                  </span>

                  <input
                    value={
                      item.title
                    }
                    maxLength={80}
                    onChange={(
                      event,
                    ) =>
                      update(
                        item.id,
                        {
                          title:
                            event.target.value,
                        },
                      )
                    }
                    placeholder="مثال: الضمان"
                    className="h-11 w-full rounded-[9px] border border-black/10 bg-white px-3 text-[11px] outline-none focus:border-black/30"
                  />
                </label>

                <label>
                  <span className="mb-1.5 block text-[9px] font-semibold text-black/55">
                    الوصف — اختياري
                  </span>

                  <input
                    value={
                      item.body
                    }
                    maxLength={220}
                    onChange={(
                      event,
                    ) =>
                      update(
                        item.id,
                        {
                          body:
                            event.target.value,
                        },
                      )
                    }
                    placeholder="مثال: ضمان لمدة شهرين"
                    className="h-11 w-full rounded-[9px] border border-black/10 bg-white px-3 text-[11px] outline-none focus:border-black/30"
                  />
                </label>
              </div>

              <div className="mt-3 flex flex-wrap items-center gap-2">
                <button
                  type="button"
                  onClick={() =>
                    update(
                      item.id,
                      {
                        isVisible:
                          !item.isVisible,
                      },
                    )
                  }
                  className="rounded-[8px] border border-black/10 px-3 py-2 text-[9px]"
                >
                  {item.isVisible
                    ? "ظاهر للعميل"
                    : "مخفي"}
                </button>

                <button
                  type="button"
                  disabled={
                    index === 0
                  }
                  onClick={() =>
                    move(
                      index,
                      -1,
                    )
                  }
                  className="rounded-[8px] border border-black/10 px-3 py-2 text-[9px] disabled:opacity-30"
                >
                  ↑ للأعلى
                </button>

                <button
                  type="button"
                  disabled={
                    index ===
                    items.length - 1
                  }
                  onClick={() =>
                    move(
                      index,
                      1,
                    )
                  }
                  className="rounded-[8px] border border-black/10 px-3 py-2 text-[9px] disabled:opacity-30"
                >
                  ↓ للأسفل
                </button>

                <button
                  type="button"
                  onClick={() =>
                    remove(
                      item.id,
                    )
                  }
                  className="mr-auto rounded-[8px] border border-red-200 px-3 py-2 text-[9px] text-red-700"
                >
                  حذف
                </button>
              </div>
            </div>
          ),
        )}
      </div>

      {error ? (
        <p className="mt-4 rounded-[9px] border border-red-200 bg-red-50 px-3 py-2 text-[10px] text-red-700">
          {error}
        </p>
      ) : null}

      {notice ? (
        <p className="mt-4 rounded-[9px] border border-emerald-200 bg-emerald-50 px-3 py-2 text-[10px] text-emerald-700">
          {notice}
        </p>
      ) : null}

      <div className="mt-4 flex items-center justify-between gap-3">
        <p className="text-[9px] text-black/40">
          {items.length}/6
        </p>

        <button
          type="button"
          onClick={() =>
            void save()
          }
          disabled={
            !tenantId ||
            loading ||
            saving
          }
          className="h-10 rounded-[9px] bg-[#080b14] px-5 text-[10px] font-semibold text-white disabled:opacity-40"
        >
          {saving
            ? "جاري الحفظ..."
            : "حفظ المميزات"}
        </button>
      </div>
    </section>
  );
}