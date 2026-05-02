import type { Contact } from "./types";

export function contactNameForNumber(contacts: Contact[], number: string): string | undefined {
  const normalized = normalizePhone(number);
  return contacts.find((contact) => normalizePhone(contact.phone) === normalized)?.name;
}

export function normalizePhone(value: string): string {
  return value.replace(/[^\d+]/g, "");
}

export function createEmptyContact(): Pick<Contact, "name" | "phone" | "favorite"> {
  return { name: "", phone: "", favorite: false };
}
