import { Pencil, Plus, Trash2 } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { createBoat, deleteBoat, getBoats, updateBoat } from "../api/boats";
import { Badge, Button, Card, Modal, PageHeader } from "../components/ui";
import type { Boat } from "../types";
import "./CrudPage.css";

const emptyForm = { callSign:"", name:"", type:"Motorboat", status:"Active", capacity:1, notes:"" };

export default function BoatsPage(){
    const [items,setItems]=useState<Boat[]>([]);
    const [search,setSearch]=useState("");
    const [form,setForm]=useState(emptyForm);
    const [editing,setEditing]=useState<Boat|null>(null);
    const [open,setOpen]=useState(false);
    const load=()=>getBoats().then(setItems);
    useEffect(()=>{void load();},[]);
    const filtered=useMemo(()=>items.filter(x=>`${x.callSign} ${x.name} ${x.type} ${x.status}`.toLowerCase().includes(search.toLowerCase())),[items,search]);

    async function save(){
        if(!form.callSign.trim()||!form.name.trim()) return;
        if(editing) await updateBoat({...editing,...form});
        else await createBoat(form);
        setOpen(false);setEditing(null);setForm(emptyForm);await load();
    }

    return <div className="eh-crud">
        <PageHeader eyebrow="FLOTTA" title="Motorcsónakok" description="Operációs egységek és műszaki állapot."
            actions={<Button icon={<Plus size={17}/>} onClick={()=>{setEditing(null);setForm(emptyForm);setOpen(true);}}>Új motorcsónak</Button>}/>
        <div className="eh-crud__toolbar"><input className="eh-crud__search" placeholder="Keresés..." value={search} onChange={e=>setSearch(e.target.value)}/></div>
        <Card><table className="eh-crud__table"><thead><tr><th>Hívójel</th><th>Név</th><th>Típus</th><th>Kapacitás</th><th>Státusz</th><th></th></tr></thead>
        <tbody>{filtered.map(item=><tr key={item.id}>
            <td><strong>{item.callSign}</strong></td><td>{item.name}</td><td>{item.type}</td><td>{item.capacity}</td>
            <td><Badge variant={item.status==="Active"?"success":"neutral"}>{item.status}</Badge></td>
            <td><div className="eh-crud__actions">
                <Button variant="ghost" icon={<Pencil size={16}/>} onClick={()=>{setEditing(item);setForm({...item,notes:item.notes??""});setOpen(true);}}>Szerkesztés</Button>
                <Button variant="danger" icon={<Trash2 size={16}/>} onClick={async()=>{if(confirm("Biztosan törlöd?")){await deleteBoat(item.id);await load();}}}>Törlés</Button>
            </div></td>
        </tr>)}</tbody></table></Card>
        <Modal open={open} title={editing?"Motorcsónak szerkesztése":"Új motorcsónak"} onClose={()=>setOpen(false)}
            footer={<><Button variant="secondary" onClick={()=>setOpen(false)}>Mégse</Button><Button onClick={()=>void save()}>Mentés</Button></>}>
            <div className="eh-form">
                <div className="eh-form__grid">
                    <label>Hívójel<input value={form.callSign} onChange={e=>setForm({...form,callSign:e.target.value})}/></label>
                    <label>Név<input value={form.name} onChange={e=>setForm({...form,name:e.target.value})}/></label>
                    <label>Típus<input value={form.type} onChange={e=>setForm({...form,type:e.target.value})}/></label>
                    <label>Kapacitás<input type="number" value={form.capacity} onChange={e=>setForm({...form,capacity:Number(e.target.value)})}/></label>
                    <label>Státusz<select value={form.status} onChange={e=>setForm({...form,status:e.target.value})}><option>Active</option><option>Inactive</option><option>Maintenance</option></select></label>
                </div>
                <label>Megjegyzés<textarea value={form.notes} onChange={e=>setForm({...form,notes:e.target.value})}/></label>
            </div>
        </Modal>
    </div>;
}
