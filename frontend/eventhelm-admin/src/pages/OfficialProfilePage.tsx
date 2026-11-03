import {
    AlertTriangle,
    ArrowLeft,
    CalendarDays,
    CheckCircle2,
    FileText,
    HeartPulse,
    Mail,
    Phone,
    Save,
    Shirt,
    ShipWheel,
    Trophy,
    UserRound,
    Users,
} from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";

import {
    getOfficialProfile,
    updateOfficialProfile,
} from "../api/officialProfiles";
import {
    Badge,
    Button,
    Card,
    PageHeader,
} from "../components/ui";
import type {
    OfficialPayload,
    OfficialProfileResponse,
} from "../types/officialProfiles";

import "./OfficialProfilePage.css";

export default function OfficialProfilePage() {
    const { officialId } = useParams<{ officialId: string }>();
    const [profile, setProfile] = useState<OfficialProfileResponse | null>(null);
    const [form, setForm] = useState<OfficialPayload | null>(null);
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [success, setSuccess] = useState<string | null>(null);

    const load = useCallback(async () => {
        if (!officialId) return;

        try {
            setLoading(true);
            const result = await getOfficialProfile(officialId);
            setProfile(result);
            setForm(toPayload(result));
            setError(null);
        } catch (loadError) {
            console.error(loadError);
            setError("A rendező adatlapja nem tölthető be.");
        } finally {
            setLoading(false);
        }
    }, [officialId]);

    useEffect(() => {
        void load();
    }, [load]);

    async function save() {
        if (!officialId || !form) return;
        if (!form.name.trim()) {
            setError("A rendező neve kötelező.");
            return;
        }

        try {
            setSaving(true);
            await updateOfficialProfile(officialId, normalizePayload(form));
            await load();
            setSuccess("A rendező adatai mentve.");
        } catch (saveError) {
            console.error(saveError);
            setError("A rendező adatainak mentése nem sikerült.");
        } finally {
            setSaving(false);
        }
    }

    if (loading) {
        return <div className="eh-official-profile"><Card><div className="eh-official-profile__state">Betöltés...</div></Card></div>;
    }

    if (!profile || !form) {
        return (
            <div className="eh-official-profile">
                <Link to="/officials" className="eh-official-profile__back">
                    <ArrowLeft size={17} /> Vissza
                </Link>
                <Card><div className="eh-official-profile__state">{error ?? "A rendező nem található."}</div></Card>
            </div>
        );
    }

    const official = profile.official;

    return (
        <div className="eh-official-profile">
            <Link to="/officials" className="eh-official-profile__back">
                <ArrowLeft size={17} /> Vissza a rendezőkhöz
            </Link>

            <PageHeader
                eyebrow="RENDEZŐ ADATLAP"
                title={official.name}
                description="Személyes adatok, szakmai minősítések, hajóvezetői képesítés és versenytörténet."
                actions={
                    <Badge variant={official.canDriveMotorboat ? "success" : "neutral"}>
                        {official.canDriveMotorboat ? "Motorcsónakot vezethet" : "Motorcsónakot nem vezethet"}
                    </Badge>
                }
            />

            {error && <div className="eh-official-profile__message eh-official-profile__message--error">{error}</div>}
            {success && <div className="eh-official-profile__message eh-official-profile__message--success">{success}</div>}

            <section className="eh-official-profile__hero">
                <div className="eh-official-profile__avatar">
                    {official.profileImageUrl ? (
                        <img src={official.profileImageUrl} alt={official.name} />
                    ) : (
                        <UserRound size={42} />
                    )}
                </div>
                <div>
                    <h2>{official.name}</h2>
                    <p>{official.club || "Klub nincs megadva"}</p>
                    <div className="eh-official-profile__contacts">
                        <span><Phone size={15} />{official.phone || "—"}</span>
                        <span><Mail size={15} />{official.email || "—"}</span>
                        <span><Shirt size={15} />{official.shirtSize || "—"}</span>
                    </div>
                </div>
            </section>

            <section className="eh-official-profile__metrics">
                <Metric icon={Trophy} label="Összes verseny" value={profile.totalCompetitions} />
                <Metric icon={CalendarDays} label="Idei versenyek" value={profile.currentYearCompetitions} />
                <Metric icon={Users} label="Betöltött szerepkörök" value={profile.roleStatistics.length} />
                <Metric icon={ShipWheel} label="Motorcsónak" value={official.canDriveMotorboat ? "Vezethet" : "Nem"} />
            </section>

            <Card
                eyebrow="RENDEZŐI ADATOK"
                title="Adatlap szerkesztése"
                action={
                    <Button icon={<Save size={17} />} loading={saving} onClick={() => void save()}>
                        Mentés
                    </Button>
                }
            >
                <div className="eh-official-profile__form">
                    <SectionTitle icon={UserRound} title="Alapadatok" />
                    <Field label="Név"><input value={form.name} onChange={(e) => update("name", e.target.value)} /></Field>
                    <Field label="Telefon"><input value={form.phone ?? ""} onChange={(e) => update("phone", e.target.value)} /></Field>
                    <Field label="E-mail"><input type="email" value={form.email ?? ""} onChange={(e) => update("email", e.target.value)} /></Field>
                    <Field label="Klub"><input value={form.club ?? ""} onChange={(e) => update("club", e.target.value)} /></Field>
                    <Field label="Pólóméret"><select value={form.shirtSize ?? ""} onChange={(e) => update("shirtSize", e.target.value)}><option value="">Nincs megadva</option><option>XS</option><option>S</option><option>M</option><option>L</option><option>XL</option><option>XXL</option></select></Field>
                    <Field label="Profilkép URL"><input value={form.profileImageUrl ?? ""} onChange={(e) => update("profileImageUrl", e.target.value)} /></Field>

                    <SectionTitle icon={Trophy} title="Minősítések" />

                    <Field label="Rendezői minősítés">
                        <input
                            value={form.organizerQualification ?? ""}
                            onChange={(e) =>
                                update("organizerQualification", e.target.value)
                            }
                            placeholder="Például NRO, IRO"
                        />
                    </Field>

                    <Field label="Versenybírói minősítés">
                        <input
                            value={form.judgeQualification ?? ""}
                            onChange={(e) =>
                                update("judgeQualification", e.target.value)
                            }
                            placeholder="Például NJ, IJ"
                        />
                    </Field>

                    <Field label="Felmérői minősítés">
                        <input
                            value={form.measurerQualification ?? ""}
                            onChange={(e) =>
                                update("measurerQualification", e.target.value)
                            }
                            placeholder="Például NM, IM"
                        />
                    </Field>
                    <SectionTitle icon={ShipWheel} title="Hajóvezetői képesítés" />
                    <Field label="Képesítés típusa"><input value={form.boatLicenseType ?? ""} onChange={(e) => update("boatLicenseType", e.target.value)} /></Field>
                    <Field label="Érvényesség"><input type="date" value={form.boatLicenseValidUntil ?? ""} onChange={(e) => update("boatLicenseValidUntil", e.target.value)} /></Field>
                    <Field label="Dokumentum URL" full><input value={form.boatLicenseDocumentUrl ?? ""} onChange={(e) => update("boatLicenseDocumentUrl", e.target.value)} /></Field>

                    <div className="eh-official-profile__drive-status">
                        {official.canDriveMotorboat ? <CheckCircle2 size={20} /> : <AlertTriangle size={20} />}
                        <div><strong>{official.canDriveMotorboat ? "Motorcsónakot vezethet" : "Motorcsónakot nem vezethet"}</strong><span>A jelvény a képesítés típusa és érvényessége alapján automatikusan számítódik.</span></div>
                    </div>

                    <SectionTitle icon={HeartPulse} title="Egészségügyi információ" />
                    <Field label="Ételallergia"><textarea rows={3} value={form.foodAllergies ?? ""} onChange={(e) => update("foodAllergies", e.target.value)} /></Field>
                    <Field label="Gyógyszerallergia"><textarea rows={3} value={form.drugAllergies ?? ""} onChange={(e) => update("drugAllergies", e.target.value)} /></Field>
                    <Field label="Egyéb egészségügyi megjegyzés" full><textarea rows={3} value={form.healthNotes ?? ""} onChange={(e) => update("healthNotes", e.target.value)} /></Field>

                    <SectionTitle icon={Phone} title="Vészhelyzeti kapcsolat" />
                    <Field label="Kapcsolattartó neve"><input value={form.emergencyContactName ?? ""} onChange={(e) => update("emergencyContactName", e.target.value)} /></Field>
                    <Field label="Telefonszám"><input value={form.emergencyContactPhone ?? ""} onChange={(e) => update("emergencyContactPhone", e.target.value)} /></Field>
                    <Field label="Kapcsolat"><input value={form.emergencyContactRelation ?? ""} onChange={(e) => update("emergencyContactRelation", e.target.value)} /></Field>
                    <Field label="Egyéb dokumentum URL"><input value={form.otherDocumentUrl ?? ""} onChange={(e) => update("otherDocumentUrl", e.target.value)} /></Field>
                </div>
            </Card>

            <section className="eh-official-profile__lower-grid">
                <Card eyebrow="STATISZTIKA" title="Betöltött szerepkörök">
                    <div className="eh-official-profile__roles">
                        {profile.roleStatistics.length === 0 ? <p>Nincs még versenystatisztika.</p> : profile.roleStatistics.map((item) => <div key={item.role}><span>{item.role}</span><strong>{item.count}×</strong></div>)}
                    </div>
                </Card>

                <Card eyebrow="DOKUMENTUMOK" title="Kapcsolódó fájlok">
                    <div className="eh-official-profile__documents">
                        <DocumentLink label="Rendezői minősítés" url={official.qualificationDocumentUrl} />
                        <DocumentLink label="Hajóvezetői képesítés" url={official.boatLicenseDocumentUrl} />
                        <DocumentLink label="Egyéb dokumentum" url={official.otherDocumentUrl} />
                    </div>
                </Card>
            </section>

            <Card eyebrow="VERSENYTÖRTÉNET" title="Korábbi versenyek">
                <div className="eh-official-profile__table-wrap">
                    <table className="eh-official-profile__table">
                        <thead><tr><th>Dátum</th><th>Verseny</th><th>Szerepkör</th><th>Helyszín</th><th></th></tr></thead>
                        <tbody>
                            {profile.competitionHistory.length === 0 ? <tr><td colSpan={5} className="eh-official-profile__empty">Nincs még versenytörténet.</td></tr> : profile.competitionHistory.map((item) => (
                                <tr key={`${item.eventId}-${item.role}`}>
                                    <td>{formatDate(item.startDate)}</td>
                                    <td><strong>{item.eventName}</strong></td>
                                    <td>{item.role}</td>
                                    <td>{item.location || "—"}</td>
                                    <td><Link to={`/events/${item.eventId}`}>Megnyitás</Link></td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            </Card>
        </div>
    );

    function update<K extends keyof OfficialPayload>(key: K, value: OfficialPayload[K]) {
        setForm((current) => current ? { ...current, [key]: value } : current);
        setSuccess(null);
    }
}

function SectionTitle({ icon: Icon, title }: { icon: typeof UserRound; title: string }) {
    return <div className="eh-official-profile__section-title"><Icon size={19} /><h3>{title}</h3></div>;
}

function Field({ label, children, full = false }: { label: string; children: React.ReactNode; full?: boolean }) {
    return <label className={full ? "eh-official-profile__field eh-official-profile__field--full" : "eh-official-profile__field"}><span>{label}</span>{children}</label>;
}

function Metric({ icon: Icon, label, value }: { icon: typeof Trophy; label: string; value: number | string }) {
    return <article className="eh-official-profile__metric"><div><Icon size={20} /></div><span>{label}</span><strong>{value}</strong></article>;
}

function DocumentLink({ label, url }: { label: string; url?: string | null }) {
    return <div className="eh-official-profile__document"><FileText size={19} /><span>{label}</span>{url ? <a href={url} target="_blank" rel="noreferrer">Megnyitás</a> : <small>Nincs feltöltve</small>}</div>;
}

function toPayload(profile: OfficialProfileResponse): OfficialPayload {
    const { id: _id, createdAt: _createdAt, canDriveMotorboat: _canDrive, ...payload } = profile.official;
    return payload;
}

function normalizePayload(payload: OfficialPayload): OfficialPayload {
    const result = { ...payload };
    for (const key of Object.keys(result) as Array<keyof OfficialPayload>) {
        const value = result[key];
        if (typeof value === "string") (result as Record<string, unknown>)[key] = value.trim() || null;
    }
    result.name = payload.name.trim();
    result.status = payload.status || "Available";
    return result;
}

function formatDate(value?: string | null): string {
    if (!value) return "—";
    return new Intl.DateTimeFormat("hu-HU", { year: "numeric", month: "short", day: "numeric" }).format(new Date(value));
}
