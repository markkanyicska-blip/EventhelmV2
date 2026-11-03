export type RadioEquipmentType = "Trunk" | "Vhf";
export type RadioEquipment = { id:string; type:RadioEquipmentType; inventoryNumber:string; callSign:string; serialNumber?:string|null; condition:string; isActive:boolean; notes?:string|null };
export type EventOfficialRadioAssignment = { id:string; eventId:string; officialId:string; role?:string|null; trunkRadioId?:string|null; vhfRadioId?:string|null; notes?:string|null; updatedAt:string };
export type RadioAssignmentView = { assignment:EventOfficialRadioAssignment; trunkRadio?:RadioEquipment|null; vhfRadio?:RadioEquipment|null };
