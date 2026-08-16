import { api } from "./client";
import type { EventOfficialRadioAssignment, RadioAssignmentView, RadioEquipment, RadioEquipmentType } from "../types/radioAssignments";
export type RadioPayload = { type:RadioEquipmentType; inventoryNumber:string; callSign:string; serialNumber?:string|null; condition:string; isActive:boolean; notes?:string|null };
export type AssignmentPayload = { officialId:string; role?:string|null; trunkRadioId?:string|null; vhfRadioId?:string|null; notes?:string|null };
export async function getRadios(){ return (await api.get<RadioEquipment[]>("/radios")).data; }
export async function createRadio(payload:RadioPayload){ return (await api.post<RadioEquipment>("/radios",payload)).data; }
export async function updateRadio(id:string,payload:RadioPayload){ return (await api.put<RadioEquipment>(`/radios/${id}`,payload)).data; }
export async function getRadioAssignments(eventId:string){ return (await api.get<RadioAssignmentView[]>(`/events/${eventId}/official-radio-assignments`)).data; }
export async function createRadioAssignment(eventId:string,payload:AssignmentPayload){ return (await api.post<EventOfficialRadioAssignment>(`/events/${eventId}/official-radio-assignments`,payload)).data; }
export async function updateRadioAssignment(eventId:string,id:string,payload:AssignmentPayload){ return (await api.put<EventOfficialRadioAssignment>(`/events/${eventId}/official-radio-assignments/${id}`,payload)).data; }
export async function deleteRadioAssignment(eventId:string,id:string){ await api.delete(`/events/${eventId}/official-radio-assignments/${id}`); }
