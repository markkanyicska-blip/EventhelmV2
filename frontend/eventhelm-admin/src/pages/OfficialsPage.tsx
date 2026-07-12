import { Pencil, Plus, Trash2 } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { createOfficial, deleteOfficial, getOfficials, updateOfficial } from "../api/officials";
import { Badge, Button, Card, Modal, PageHeader } from "../components/ui";
import type { Official } from "../types";
import "./CrudPage.css";

const emptyForm = { name:"", phone:"", email:"", role:"Rendező", status:"Available" };

export default function OfficialsPage(){
    const [items,setItems]=useState<Official[]>([]);
    const [search,setSearch]=useState("");
    const [form,setForm]=useState(emptyForm);
    const [editing,setEditing]=useState<Official|null>(null);
    const [open,setOpen]=useState(false);
    const load=()=>getOfficials().then(setItems);
    useEffect(()=>{void load();},[]);
    const filtered=useMemo(()=>items.filter(x=>`${x.name} ${x.role} ${x.status}`.toLowerCase().includes(search.toLowerCase())),[items,search]);

    async function save(){
        if(!form.name.trim()) return;
        if(editing) await updateOfficial({...editing,...form});
        else await createOfficial(form);
        setOpen(false);setEditing(null);setForm(emptyForm);await load();
    }

    return <div className="eh-crud">
        <PageHeader eyebrow="SZEMÉLYZET" title="Rendezők" description="A versenyirányítás személyi állományának kezelése."
            actions={<Button icon={<Plus size={17}/>} onClick={()=>{setEditing(null);setForm(emptyForm);setOpen(true);}}>Új rendező</Button>}/>
        <div className="eh-crud__toolbar"><input className="eh-crud__search" placeholder="Keresés..." value={search} onChange={e=>setSearch(e.target.value)}/></div>
        <Card><table className="eh-crud__table"><thead><tr><th>Név</th><th>Szerepkör</th><th>Telefon</th><th>E-mail</th><th>Státusz</th><th></th></tr></thead>
        <tbody>{filtered.map(item=><tr key={item.id}>
            <td><strong>{item.name}</strong></td><td>{item.role}</td><td>{item.phone||"—"}</td><td>{item.email||"—"}</td>
            <td><Badge variant={["Available","Active"].includes(item.status)?"success":"neutral"}>{item.status}</Badge></td>
            <td><div className="eh-crud__actions">
                <Button variant="ghost" icon={<Pencil size={16}/>} onClick={()=>{setEditing(item);setForm({name:item.name,phone:item.phone??"",email:item.email??"",role:item.role,status:item.status});setOpen(true);}}>Szerkesztés</Button>
                <Button variant="danger" icon={<Trash2 size={16}/>} onClick={async()=>{if(confirm("Biztosan törlöd?")){await deleteOfficial(item.id);await load();}}}>Törlés</Button>
            </div></td>
        </tr>)}</tbody></table></Card>
        <Modal open={open} title={editing?"Rendező szerkesztése":"Új rendező"} onClose={()=>setOpen(false)}
            footer={<><Button variant="secondary" onClick={()=>setOpen(false)}>Mégse</Button><Button onClick={()=>void save()}>Mentés</Button></>}>
            <div className="eh-form"><div className="eh-form__grid">
                <label>Név<input value={form.name} onChange={e=>setForm({...form,name:e.target.value})}/></label>
                <label>Szerepkör<input value={form.role} onChange={e=>setForm({...form,role:e.target.value})}/></label>
                <label>Telefon<input value={form.phone} onChange={e=>setForm({...form,phone:e.target.value})}/></label>
                <label>E-mail<input value={form.email} onChange={e=>setForm({...form,email:e.target.value})}/></label>
                <label>Státusz<select value={form.status} onChange={e=>setForm({...form,status:e.target.value})}><option>Available</option><option>Active</option><option>Unavailable</option></select></label>
            </div></div>
        </Modal>
    </div>;
}
