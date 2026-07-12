import { Pencil, Plus, Trash2 } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { createUser, deleteUser, getUsers, updateUser } from "../api/users";
import { Badge, Button, Card, Modal, PageHeader } from "../components/ui";
import type { User } from "../types";
import "./CrudPage.css";

const emptyForm = { name:"", email:"", phone:"", role:"User", isActive:true };

export default function UsersPage(){
    const [items,setItems]=useState<User[]>([]);
    const [search,setSearch]=useState("");
    const [form,setForm]=useState(emptyForm);
    const [editing,setEditing]=useState<User|null>(null);
    const [open,setOpen]=useState(false);
    const load=()=>getUsers().then(setItems);
    useEffect(()=>{void load();},[]);
    const filtered=useMemo(()=>items.filter(x=>`${x.name} ${x.email} ${x.role}`.toLowerCase().includes(search.toLowerCase())),[items,search]);

    async function save(){
        if(!form.name.trim()||!form.email.trim()) return;
        if(editing) await updateUser({...editing,...form});
        else await createUser(form);
        setOpen(false);setEditing(null);setForm(emptyForm);await load();
    }

    return <div className="eh-crud">
        <PageHeader eyebrow="HOZZÁFÉRÉS" title="Felhasználók" description="Felhasználói fiókok és jogosultságok kezelése."
            actions={<Button icon={<Plus size={17}/>} onClick={()=>{setEditing(null);setForm(emptyForm);setOpen(true);}}>Új felhasználó</Button>}/>
        <div className="eh-crud__toolbar"><input className="eh-crud__search" placeholder="Keresés..." value={search} onChange={e=>setSearch(e.target.value)}/></div>
        <Card><table className="eh-crud__table"><thead><tr><th>Név</th><th>E-mail</th><th>Telefon</th><th>Szerepkör</th><th>Állapot</th><th></th></tr></thead>
        <tbody>{filtered.map(item=><tr key={item.id}>
            <td><strong>{item.name}</strong></td><td>{item.email}</td><td>{item.phone||"—"}</td><td>{item.role}</td>
            <td><Badge variant={item.isActive?"success":"danger"}>{item.isActive?"Aktív":"Inaktív"}</Badge></td>
            <td><div className="eh-crud__actions">
                <Button variant="ghost" icon={<Pencil size={16}/>} onClick={()=>{setEditing(item);setForm({name:item.name,email:item.email,phone:item.phone??"",role:item.role,isActive:item.isActive});setOpen(true);}}>Szerkesztés</Button>
                <Button variant="danger" icon={<Trash2 size={16}/>} onClick={async()=>{if(confirm("Biztosan törlöd?")){await deleteUser(item.id);await load();}}}>Törlés</Button>
            </div></td>
        </tr>)}</tbody></table></Card>
        <Modal open={open} title={editing?"Felhasználó szerkesztése":"Új felhasználó"} onClose={()=>setOpen(false)}
            footer={<><Button variant="secondary" onClick={()=>setOpen(false)}>Mégse</Button><Button onClick={()=>void save()}>Mentés</Button></>}>
            <div className="eh-form"><div className="eh-form__grid">
                <label>Név<input value={form.name} onChange={e=>setForm({...form,name:e.target.value})}/></label>
                <label>E-mail<input value={form.email} onChange={e=>setForm({...form,email:e.target.value})}/></label>
                <label>Telefon<input value={form.phone} onChange={e=>setForm({...form,phone:e.target.value})}/></label>
                <label>Szerepkör<input value={form.role} onChange={e=>setForm({...form,role:e.target.value})}/></label>
                <label>Állapot<select value={String(form.isActive)} onChange={e=>setForm({...form,isActive:e.target.value==="true"})}><option value="true">Aktív</option><option value="false">Inaktív</option></select></label>
            </div></div>
        </Modal>
    </div>;
}
