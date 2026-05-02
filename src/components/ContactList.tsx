import { Heart, Pencil, PhoneCall, Plus, Save, Star, Trash2, X } from "lucide-react";
import { useMemo, useState } from "react";
import { createEmptyContact } from "../lib/contacts";
import type { Contact } from "../lib/types";

interface ContactListProps {
  contacts: Contact[];
  onDial: (phone: string) => Promise<unknown>;
  onCreate: (contact: Pick<Contact, "name" | "phone" | "favorite">) => Promise<unknown>;
  onUpdate: (contact: Contact) => Promise<unknown>;
  onDelete: (contactId: string) => Promise<unknown>;
}

export function ContactList({ contacts, onDial, onCreate, onUpdate, onDelete }: ContactListProps) {
  const [query, setQuery] = useState("");
  const [editing, setEditing] = useState<Contact | null>(null);
  const [draft, setDraft] = useState(createEmptyContact());

  const filtered = useMemo(() => {
    const normalized = query.trim().toLowerCase();
    return contacts
      .filter((contact) => !normalized || contact.name.toLowerCase().includes(normalized) || contact.phone.includes(normalized))
      .sort((a, b) => Number(b.favorite) - Number(a.favorite) || a.name.localeCompare(b.name));
  }, [contacts, query]);

  function startCreate() {
    setEditing(null);
    setDraft(createEmptyContact());
  }

  function startEdit(contact: Contact) {
    setEditing(contact);
    setDraft({ name: contact.name, phone: contact.phone, favorite: contact.favorite });
  }

  async function save() {
    if (!draft.name.trim() || !draft.phone.trim()) {
      return;
    }

    if (editing) {
      await onUpdate({ ...editing, ...draft });
    } else {
      await onCreate(draft);
    }
    startCreate();
  }

  return (
    <aside className="glass-panel flex flex-col rounded-2xl p-5">
      <div className="mb-4 flex items-center justify-between">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.22em] text-cyan-200/80">Contacts</p>
          <h2 className="mt-1 text-xl font-semibold text-white">Local list</h2>
        </div>
        <button
          type="button"
          onClick={startCreate}
          className="grid h-10 w-10 place-items-center rounded-full border border-line bg-white/8 text-slate-200 transition hover:border-cyan-300/40 hover:bg-cyan-300/10"
          aria-label="Create contact"
        >
          <Plus size={18} />
        </button>
      </div>

      <input
        value={query}
        onChange={(event) => setQuery(event.target.value)}
        placeholder="Search name or number"
        className="mb-4 h-11 rounded-xl border border-line bg-slate-950/38 px-4 text-sm text-white outline-none transition placeholder:text-slate-500 focus:border-cyan-300/50"
      />

      <div className="mb-4 rounded-2xl border border-line bg-white/5 p-4">
        <div className="grid gap-3">
          <input
            value={draft.name}
            onChange={(event) => setDraft((current) => ({ ...current, name: event.target.value }))}
            placeholder="Contact name"
            className="h-10 rounded-xl border border-line bg-slate-950/45 px-3 text-sm text-white outline-none focus:border-cyan-300/50"
          />
          <input
            value={draft.phone}
            onChange={(event) => setDraft((current) => ({ ...current, phone: event.target.value }))}
            placeholder="Phone number"
            className="h-10 rounded-xl border border-line bg-slate-950/45 px-3 text-sm text-white outline-none focus:border-cyan-300/50"
          />
          <div className="flex items-center justify-between">
            <button
              type="button"
              onClick={() => setDraft((current) => ({ ...current, favorite: !current.favorite }))}
              className={`flex items-center gap-2 rounded-xl border px-3 py-2 text-sm transition ${
                draft.favorite ? "border-cyan-300/40 bg-cyan-300/10 text-cyan-100" : "border-line bg-white/5 text-slate-300"
              }`}
            >
              <Heart size={16} />
              Favorite
            </button>
            <div className="flex gap-2">
              <button
                type="button"
                onClick={startCreate}
                className="grid h-10 w-10 place-items-center rounded-xl border border-line bg-white/5 text-slate-300 transition hover:bg-white/10"
                aria-label="Clear contact form"
              >
                <X size={16} />
              </button>
              <button
                type="button"
                onClick={() => void save()}
                className="grid h-10 w-10 place-items-center rounded-xl bg-cyan-300 text-slate-950 transition hover:bg-cyan-200"
                aria-label="Save contact"
              >
                <Save size={16} />
              </button>
            </div>
          </div>
        </div>
      </div>

      <div className="space-y-3">
        {filtered.map((contact) => (
          <article key={contact.id} className="rounded-2xl border border-line bg-white/6 p-4">
            <div className="flex items-start gap-3">
              <div className="grid h-11 w-11 place-items-center rounded-xl bg-cyan-300/10 text-cyan-200">
                {contact.favorite ? <Star size={18} /> : contact.name.slice(0, 1).toUpperCase()}
              </div>
              <div className="min-w-0 flex-1">
                <p className="truncate font-semibold text-white">{contact.name}</p>
                <p className="truncate text-sm text-slate-400">{contact.phone}</p>
              </div>
            </div>
            <div className="mt-3 grid grid-cols-3 gap-2">
              <button type="button" onClick={() => void onDial(contact.phone)} className="rounded-xl bg-emerald-400 py-2 text-emerald-950 transition hover:bg-emerald-300" aria-label={`Call ${contact.name}`}>
                <PhoneCall className="mx-auto" size={17} />
              </button>
              <button type="button" onClick={() => startEdit(contact)} className="rounded-xl border border-line bg-white/7 py-2 text-slate-300 transition hover:bg-white/12" aria-label={`Edit ${contact.name}`}>
                <Pencil className="mx-auto" size={17} />
              </button>
              <button type="button" onClick={() => void onDelete(contact.id)} className="rounded-xl border border-rose-300/20 bg-rose-400/10 py-2 text-rose-200 transition hover:bg-rose-400/16" aria-label={`Delete ${contact.name}`}>
                <Trash2 className="mx-auto" size={17} />
              </button>
            </div>
          </article>
        ))}
      </div>
    </aside>
  );
}
