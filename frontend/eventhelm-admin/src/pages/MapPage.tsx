import { useState } from "react";
import {
    MapContainer,
    Marker,
    Popup,
    Polyline,
    TileLayer,
    useMapEvents,
} from "react-leaflet";
import {
    divIcon,
    type DragEndEvent,
    type LatLngExpression,
    type LeafletMouseEvent,
    type PathOptions,
} from "leaflet";

import "leaflet/dist/leaflet.css";
import "./MapPage.css";

type Coordinate = [number, number];

type MapBoat = {
    id: string;
    name: string;
    registrationNumber: string;
    status: "active" | "standby" | "offline";
    position: Coordinate;
    task: string;
    crew: string[];
};

type CourseMark = {
    id: string;
    name: string;
    type: "mark" | "start" | "finish";
    position: Coordinate;
    description: string;
};

type MapInteractionHandlerProps = {
    onAddMark: (position: Coordinate) => void;
};

const balatoniCenter: Coordinate = [
    46.9565,
    17.888,
];

const boats: MapBoat[] = [
    {
        id: "boat-1",
        name: "Sirály",
        registrationNumber: "H-12345",
        status: "active",
        position: [46.9204, 17.892],
        task: "Tihanyi pályajel biztosítása",
        crew: ["Rendező 1", "Rendező 2"],
    },
    {
        id: "boat-2",
        name: "Vihar",
        registrationNumber: "H-67890",
        status: "standby",
        position: [46.988, 18.043],
        task: "Kenesei pályajel",
        crew: ["Rendező 3"],
    },
];

const initialCourseMarks: CourseMark[] = [
    {
        id: "mark-1",
        name: "Kenesei pályajel",
        type: "mark",
        position: [47.0231667, 18.10535],
        description: "1. pályajel",
    },
    {
        id: "mark-2",
        name: "Balatonföldvári pályajel",
        type: "mark",
        position: [46.9200667, 18.03985],
        description: "2. pályajel",
    },
    {
        id: "mark-3",
        name: "Tihanyi pályajel",
        type: "mark",
        position: [46.9186, 17.9091667],
        description: "3. pályajel",
    },
    {
        id: "start",
        name: "Rajt",
        type: "start",
        position: [46.956, 17.882],
        description: "Tervezett rajtterület",
    },
    {
        id: "finish",
        name: "Cél",
        type: "finish",
        position: [46.963, 17.9],
        description: "Tervezett célterület",
    },
];

function MapInteractionHandler({
    onAddMark,
}: MapInteractionHandlerProps) {
    useMapEvents({
        contextmenu(event: LeafletMouseEvent) {
            onAddMark([
                event.latlng.lat,
                event.latlng.lng,
            ]);
        },
    });

    return null;
}

function createMarkIcon(type: CourseMark["type"]) {
    const className =
        type === "start"
            ? "map-marker map-marker--start"
            : type === "finish"
                ? "map-marker map-marker--finish"
                : "map-marker map-marker--course";

    const label =
        type === "start"
            ? "R"
            : type === "finish"
                ? "C"
                : "";

    return divIcon({
        className: "",
        html: `<div class="${className}">${label}</div>`,
        iconSize: [26, 26],
        iconAnchor: [13, 13],
        popupAnchor: [0, -15],
    });
}

function createBoatIcon(status: MapBoat["status"]) {
    const statusClass =
        status === "active"
            ? "map-boat-marker--active"
            : status === "standby"
                ? "map-boat-marker--standby"
                : "map-boat-marker--offline";

    return divIcon({
        className: "",
        html: `
            <div class="map-boat-marker ${statusClass}">
                🛥️
            </div>
        `,
        iconSize: [42, 42],
        iconAnchor: [21, 21],
        popupAnchor: [0, -22],
    });
}

function getBoatStatusText(
    status: MapBoat["status"],
): string {
    switch (status) {
        case "active":
            return "Aktív";
        case "standby":
            return "Készenlét";
        default:
            return "Offline";
    }
}

export default function MapPage() {
    const [courseMarks, setCourseMarks] =
        useState<CourseMark[]>(initialCourseMarks);

    function addCourseMark(position: Coordinate) {
        setCourseMarks((currentMarks) => {
            const markNumber =
                currentMarks.filter(
                    (mark) => mark.type === "mark",
                ).length + 1;

            const newMark: CourseMark = {
                id: crypto.randomUUID(),
                name: `${markNumber}. pályajel`,
                type: "mark",
                position,
                description:
                    "Jobb kattintással létrehozott pályajel",
            };

            return [...currentMarks, newMark];
        });
    }

    function updateMarkPosition(
        markId: string,
        event: DragEndEvent,
    ) {
        const marker = event.target;
        const position = marker.getLatLng();

        setCourseMarks((currentMarks) =>
            currentMarks.map((mark) =>
                mark.id === markId
                    ? {
                        ...mark,
                        position: [
                            position.lat,
                            position.lng,
                        ],
                    }
                    : mark,
            ),
        );
    }

    function deleteCourseMark(markId: string) {
        setCourseMarks((currentMarks) =>
            currentMarks.filter(
                (mark) => mark.id !== markId,
            ),
        );
    }

    const courseLine: LatLngExpression[] =
        courseMarks.map((mark) => mark.position);

    const courseStyle: PathOptions = {
        color: "#2563eb",
        weight: 3,
        dashArray: "8 8",
    };

    return (
        <div className="eventhelm-map-page">
            <div className="eventhelm-map-page__header">
                <div>
                    <span className="eventhelm-map-page__eyebrow">
                        OPERÁCIÓS TÉRKÉP
                    </span>

                    <h1>Versenyterület</h1>

                    <p>
                        A pályajelek áthúzhatók. Új pályajel
                        elhelyezéséhez kattints jobb egérgombbal
                        a térképre.
                    </p>
                </div>

                <div className="eventhelm-map-page__legend">
                    <span>
                        <i className="legend-dot legend-dot--green" />
                        Aktív hajó
                    </span>

                    <span>
                        <i className="legend-dot legend-dot--yellow" />
                        Pályajel
                    </span>

                    <span>
                        <i className="legend-dot legend-dot--blue" />
                        Rajt
                    </span>

                    <span>
                        <i className="legend-dot legend-dot--red" />
                        Cél
                    </span>
                </div>
            </div>

            <div className="eventhelm-map-page__map-wrapper">
                <MapContainer
                    center={balatoniCenter}
                    zoom={10}
                    scrollWheelZoom
                    className="eventhelm-map-page__map"
                >
                    <TileLayer
                        attribution="&copy; OpenStreetMap contributors"
                        url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
                    />

                    <MapInteractionHandler
                        onAddMark={addCourseMark}
                    />

                    {courseLine.length > 1 && (
                        <Polyline
                            positions={courseLine}
                            pathOptions={courseStyle}
                        />
                    )}

                    {courseMarks.map((mark) => (
                        <Marker
                            key={mark.id}
                            position={mark.position}
                            icon={createMarkIcon(mark.type)}
                            draggable
                            eventHandlers={{
                                dragend: (event) =>
                                    updateMarkPosition(
                                        mark.id,
                                        event,
                                    ),
                            }}
                        >
                            <Popup>
                                <div className="eventhelm-map-popup">
                                    <strong>{mark.name}</strong>

                                    <span>
                                        {mark.description}
                                    </span>

                                    <span>
                                        Szélesség:{" "}
                                        {mark.position[0].toFixed(6)}
                                    </span>

                                    <span>
                                        Hosszúság:{" "}
                                        {mark.position[1].toFixed(6)}
                                    </span>

                                    {mark.type === "mark" && (
                                        <button
                                            type="button"
                                            className="map-popup-delete-button"
                                            onClick={() =>
                                                deleteCourseMark(
                                                    mark.id,
                                                )
                                            }
                                        >
                                            Pályajel törlése
                                        </button>
                                    )}
                                </div>
                            </Popup>
                        </Marker>
                    ))}

                    {boats.map((boat) => (
                        <Marker
                            key={boat.id}
                            position={boat.position}
                            icon={createBoatIcon(boat.status)}
                        >
                            <Popup>
                                <div className="eventhelm-map-popup">
                                    <strong>{boat.name}</strong>

                                    <span>
                                        Lajstromszám:{" "}
                                        {boat.registrationNumber}
                                    </span>

                                    <span>
                                        Feladat: {boat.task}
                                    </span>

                                    <span>
                                        Állapot:{" "}
                                        {getBoatStatusText(
                                            boat.status,
                                        )}
                                    </span>

                                    <span>
                                        Személyzet:{" "}
                                        {boat.crew.join(", ")}
                                    </span>
                                </div>
                            </Popup>
                        </Marker>
                    ))}
                </MapContainer>
            </div>
        </div>
    );
}