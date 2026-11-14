"""Icon generator of the game (items, skills, statuses, window buttons, emotes).

Every icon is drawn here as SVG (own art, no third-party assets) and rendered to a 128x128 opaque PNG by render.js.
Run:  python3 tools/IconGen/icons.py  &&  node tools/IconGen/render.js
Output: My project/Assets/_Project/Resources/Icons/<group>/<id>.png   (group: items, skills, statuses, windows, emotes)
To add an icon: write a function returning SVG shapes and register it in ICONS under the content's Icon id.
"""
import hashlib
import os

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "svg")
ASSET_ROOT = "Assets/_Project/Resources/Icons"   # relative to the Unity project
UNITY_PROJECT = os.path.join(HERE, "..", "..", "My project")
INK = "#0b0d13"          # outline of every glyph
OUTLINE = f'stroke="{INK}" stroke-width="3" stroke-linejoin="round" stroke-linecap="round"'
THIN = f'stroke="{INK}" stroke-width="2" stroke-linejoin="round" stroke-linecap="round"'

_uid = 0


def uid(prefix="g"):
    global _uid
    _uid += 1
    return f"{prefix}{_uid}"


def lin(stops, x1=0, y1=0, x2=0, y2=1):
    """Linear gradient: returns (defs, 'url(#id)')."""
    i = uid()
    body = "".join(f'<stop offset="{o}" stop-color="{c}"/>' for o, c in stops)
    return f'<linearGradient id="{i}" x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}">{body}</linearGradient>', f"url(#{i})"


def rad(stops, cx=0.5, cy=0.5, r=0.5):
    i = uid()
    body = "".join(f'<stop offset="{o}" stop-color="{c}"/>' for o, c in stops)
    return f'<radialGradient id="{i}" cx="{cx}" cy="{cy}" r="{r}">{body}</radialGradient>', f"url(#{i})"


def icon(bg, shapes, glow=None):
    """Wraps shapes into a 128x128 icon: gradient background, soft vignette, optional coloured glow behind the glyph."""
    d1, bgfill = rad([(0, bg[0]), (1, bg[1])], 0.5, 0.42, 0.75)
    d2, vig = rad([(0.55, "#00000000"), (1, "#00000099")], 0.5, 0.5, 0.72)
    glow_svg = ""
    d3 = ""
    if glow:
        d3, gf = rad([(0, glow + "cc"), (0.6, glow + "33"), (1, glow + "00")])
        glow_svg = f'<circle cx="64" cy="64" r="56" fill="{gf}"/>'
    return (f'<svg xmlns="http://www.w3.org/2000/svg" width="128" height="128" viewBox="0 0 128 128">'
            f'<defs>{d1}{d2}{d3}{"".join(DEFS)}</defs>'
            f'<rect width="128" height="128" fill="{bgfill}"/>{glow_svg}{shapes}'
            f'<rect width="128" height="128" fill="{vig}"/>'
            f'<rect x="1.5" y="1.5" width="125" height="125" fill="none" stroke="#ffffff22" stroke-width="3"/></svg>')


DEFS = []


def g(defs_and_fill):
    """Registers a gradient's defs for the current icon and returns its fill reference."""
    d, f = defs_and_fill
    DEFS.append(d)
    return f


# ---------- palettes ----------
BG_WEAPON = ("#3b4a63", "#141a26")
BG_ARMOR = ("#4a4f5c", "#16181f")
BG_CLOTH = ("#4b3d6b", "#17122a")
BG_JEWEL = ("#5a4a2a", "#1c1509")
BG_POTION = ("#24474d", "#0b1719")
BG_MATERIAL = ("#4d4033", "#17120d")
BG_QUEST = ("#5b5237", "#1b170c")
BG_WINDOW = ("#2f4468", "#0e1626")
BG_FIRE = ("#6b2a12", "#1c0803")
BG_ARCANE = ("#3a2f7a", "#0e0a26")
BG_NATURE = ("#1f5a35", "#07190e")
BG_BLOOD = ("#5e1418", "#190305")
BG_STEEL = ("#46505f", "#12151b")
BG_BEAST = ("#4f3a2a", "#150d07")
BG_DEBUFF = ("#5a2020", "#170707")
BG_BUFF = ("#1f4d5a", "#07151a")

STEEL = [(0, "#f2f6fb"), (0.5, "#aeb9c9"), (1, "#5d6878")]
IRON = [(0, "#d7dbe2"), (0.5, "#8d94a1"), (1, "#4b515c")]
BLUESTEEL = [(0, "#e6f1ff"), (0.5, "#8fb0dc"), (1, "#3f5f92")]
GOLD = [(0, "#fff3b0"), (0.5, "#f0c040"), (1, "#a36a12")]
WOOD = [(0, "#c8935a"), (0.5, "#8d5a2e"), (1, "#553314")]
LEATHER = [(0, "#c99a6a"), (0.5, "#8f6238"), (1, "#56361b")]
CLOTH = [(0, "#b9a3ef"), (0.5, "#7a5fd0"), (1, "#3f2c86")]
CLOTH2 = [(0, "#9fd0ee"), (0.5, "#5a93c8"), (1, "#2c4f86")]


# ---------- weapons ----------
def sword(blade=STEEL, guard=GOLD, grip=LEATHER, gem=None):
    b = g(lin(blade, 0, 0, 1, 0))
    gd = g(lin(guard))
    gr = g(lin(grip, 0, 0, 1, 0))
    gem_svg = f'<circle cx="64" cy="90" r="4.5" fill="{gem}" {THIN}/>' if gem else ""
    return (f'<g transform="rotate(45 64 64)">'
            f'<path d="M64 4 L74 22 L74 86 L54 86 L54 22 Z" fill="{b}" {OUTLINE}/>'
            f'<path d="M64 12 L64 84" stroke="#ffffffaa" stroke-width="2.5"/>'
            f'<path d="M69 24 L69 84" stroke="#00000033" stroke-width="3"/>'
            f'<rect x="38" y="84" width="52" height="11" rx="5" fill="{gd}" {OUTLINE}/>{gem_svg}'
            f'<rect x="58" y="95" width="12" height="20" rx="3" fill="{gr}" {OUTLINE}/>'
            f'<path d="M58 101 h12 M58 107 h12" stroke="{INK}" stroke-width="1.5" opacity="0.6"/>'
            f'<circle cx="64" cy="119" r="7" fill="{gd}" {OUTLINE}/></g>')


def axe():
    h = g(lin(WOOD, 0, 0, 1, 0))
    b = g(lin(STEEL, 0, 0, 1, 1))
    return (f'<g transform="rotate(40 64 64)">'
            f'<rect x="59" y="10" width="10" height="110" rx="4" fill="{h}" {OUTLINE}/>'
            f'<path d="M69 18 C96 14 108 30 106 46 C104 60 92 66 69 60 Z" fill="{b}" {OUTLINE}/>'
            f'<path d="M59 18 C32 14 20 30 22 46 C24 60 36 66 59 60 Z" fill="{b}" {OUTLINE}/>'
            f'<path d="M100 28 C104 38 102 50 96 56" stroke="#ffffffcc" stroke-width="3" fill="none"/>'
            f'<path d="M28 28 C24 38 26 50 32 56" stroke="#ffffffcc" stroke-width="3" fill="none"/>'
            f'<rect x="56" y="14" width="16" height="50" rx="3" fill="{g(lin(GOLD))}" {OUTLINE}/>'
            f'<path d="M59 96 h10 M59 104 h10" stroke="{INK}" stroke-width="1.5" opacity="0.6"/></g>')


def staff():
    w = g(lin(WOOD, 0, 0, 1, 0))
    orb = g(rad([(0, "#ffffff"), (0.35, "#b9f1ff"), (1, "#2a7fd8")], 0.38, 0.35, 0.7))
    return (f'<g transform="rotate(38 64 64)">'
            f'<rect x="60" y="34" width="8" height="90" rx="4" fill="{w}" {OUTLINE}/>'
            f'<path d="M52 36 C50 18 58 10 64 10 C70 10 78 18 76 36 C72 30 68 30 64 36 C60 30 56 30 52 36 Z" fill="{g(lin(GOLD))}" {OUTLINE}/>'
            f'<circle cx="64" cy="22" r="11" fill="{orb}" {OUTLINE}/>'
            f'<circle cx="60" cy="18" r="3" fill="#ffffff"/>'
            f'<rect x="57" y="62" width="14" height="6" rx="2" fill="{g(lin(GOLD))}" {THIN}/></g>')


def shield(body, rim, emblem):
    f = g(lin(body, 0, 0, 1, 1))
    r = g(lin(rim))
    return (f'<path d="M64 12 C82 20 98 20 108 22 C108 70 94 102 64 118 C34 102 20 70 20 22 C30 20 46 20 64 12 Z" fill="{r}" {OUTLINE}/>'
            f'<path d="M64 24 C78 30 90 30 98 31 C97 68 86 92 64 106 C42 92 31 68 30 31 C38 30 50 30 64 24 Z" fill="{f}" {THIN}/>'
            f'{emblem}'
            f'<path d="M36 36 C44 34 52 33 60 30" stroke="#ffffff66" stroke-width="4" fill="none" stroke-linecap="round"/>')


def emblem_boss():
    return f'<circle cx="64" cy="62" r="13" fill="{g(lin(IRON))}" {OUTLINE}/><circle cx="60" cy="58" r="3.5" fill="#ffffffaa"/>'


def emblem_lion():
    return (f'<path d="M64 44 L70 58 L85 60 L74 70 L77 85 L64 77 L51 85 L54 70 L43 60 L58 58 Z" fill="{g(lin(GOLD))}" {OUTLINE}/>')


# ---------- armour ----------
def helm(metal, trim, plume=None):
    m = g(lin(metal, 0, 0, 1, 1))
    t = g(lin(trim))
    plume_svg = (f'<path d="M64 20 C70 2 96 4 104 22 C92 16 82 20 76 30 Z" fill="{plume}" {OUTLINE}/>' if plume else "")
    return (f'{plume_svg}<path d="M22 78 C20 40 40 18 64 18 C88 18 108 40 106 78 L106 104 L80 104 L80 84 L72 84 L72 104 L56 104 L56 84 L48 84 L48 104 L22 104 Z" fill="{m}" {OUTLINE}/>'
            f'<path d="M22 70 C40 64 88 64 106 70 L106 80 C88 74 40 74 22 80 Z" fill="{t}" {THIN}/>'
            f'<path d="M64 18 L64 66" stroke="{INK}" stroke-width="3"/><path d="M60 20 L60 64" stroke="#ffffff55" stroke-width="3"/>'
            f'<path d="M34 46 C40 32 50 26 58 24" stroke="#ffffff88" stroke-width="4" fill="none" stroke-linecap="round"/>')


def hood():
    c = g(lin(CLOTH, 0, 0, 1, 1))
    return (f'<path d="M64 12 C30 14 16 50 22 104 L46 112 C40 82 46 58 64 54 C82 58 88 82 82 112 L106 104 C112 50 98 14 64 12 Z" fill="{c}" {OUTLINE}/>'
            f'<path d="M46 112 C40 82 46 58 64 54 C82 58 88 82 82 112 Z" fill="#120d22" {THIN}/>'
            f'<path d="M36 40 C44 26 54 20 62 19" stroke="#ffffff66" stroke-width="4" fill="none" stroke-linecap="round"/>'
            f'<path d="M22 104 L46 112 M106 104 L82 112" stroke="{g(lin(GOLD))}" stroke-width="5" stroke-linecap="round"/>')


def breastplate(metal, trim):
    m = g(lin(metal, 0, 0, 1, 1))
    t = g(lin(trim))
    return (f'<path d="M30 20 L50 14 C56 22 72 22 78 14 L98 20 L112 44 L96 54 L96 104 C76 116 52 116 32 104 L32 54 L16 44 Z" fill="{m}" {OUTLINE}/>'
            f'<path d="M50 14 C56 22 72 22 78 14 L84 16 C76 30 52 30 44 16 Z" fill="{t}" {THIN}/>'
            f'<path d="M64 30 L64 108" stroke="{INK}" stroke-width="2.5"/>'
            f'<path d="M36 66 C52 74 76 74 92 66 M36 84 C52 92 76 92 92 84" stroke="{INK}" stroke-width="2.5" fill="none"/>'
            f'<path d="M40 30 C44 40 44 52 40 60" stroke="#ffffff77" stroke-width="4" fill="none" stroke-linecap="round"/>'
            f'<circle cx="24" cy="42" r="4" fill="{t}" {THIN}/><circle cx="104" cy="42" r="4" fill="{t}" {THIN}/>')


def robe():
    c = g(lin(CLOTH, 0, 0, 1, 1))
    return (f'<path d="M44 14 L84 14 L112 40 L98 56 L90 48 L96 114 L32 114 L38 48 L30 56 L16 40 Z" fill="{c}" {OUTLINE}/>'
            f'<path d="M52 14 L64 34 L76 14" fill="#120d22" {THIN}/>'
            f'<path d="M64 34 L64 114" stroke="{g(lin(GOLD))}" stroke-width="5"/>'
            f'<path d="M36 78 C52 84 76 84 92 78" stroke="{g(lin(GOLD))}" stroke-width="5" fill="none"/>'
            f'<path d="M40 30 C36 44 38 60 40 70" stroke="#ffffff55" stroke-width="4" fill="none" stroke-linecap="round"/>')


def glove(mat, cuff, plates=False):
    m = g(lin(mat, 0, 0, 1, 1))
    c = g(lin(cuff))
    pl = (f'<path d="M40 50 L80 50 M42 62 L78 62" stroke="{INK}" stroke-width="2.5"/>' if plates else "")
    return (f'<path d="M38 70 L36 34 C36 26 46 26 46 34 L47 22 C47 14 57 14 57 22 L58 18 C58 10 68 10 68 18 L69 24 C69 16 79 16 79 24 L80 56 L88 44 C92 38 100 42 97 50 L84 82 L84 92 L40 92 Z" fill="{m}" {OUTLINE}/>'
            f'{pl}<rect x="34" y="90" width="56" height="24" rx="6" fill="{c}" {OUTLINE}/>'
            f'<path d="M42 40 L42 66" stroke="#ffffff66" stroke-width="3.5" stroke-linecap="round"/>')


def boot(mat, sole, plates=False):
    m = g(lin(mat, 0, 0, 1, 1))
    s = g(lin(sole))
    pl = (f'<path d="M44 40 L76 40 M44 56 L76 56" stroke="{INK}" stroke-width="2.5"/>' if plates else "")
    return (f'<path d="M42 14 L78 14 L78 70 C96 72 110 82 112 98 L112 104 L40 104 Z" fill="{m}" {OUTLINE}/>'
            f'{pl}<rect x="36" y="102" width="80" height="12" rx="4" fill="{s}" {OUTLINE}/>'
            f'<rect x="38" y="10" width="44" height="12" rx="4" fill="{s}" {OUTLINE}/>'
            f'<path d="M50 28 L50 84" stroke="#ffffff55" stroke-width="4" stroke-linecap="round"/>')


def legs(mat, trim, plates=False):
    m = g(lin(mat, 0, 0, 1, 1))
    t = g(lin(trim))
    pl = (f'<ellipse cx="46" cy="72" rx="11" ry="9" fill="{t}" {THIN}/><ellipse cx="82" cy="72" rx="11" ry="9" fill="{t}" {THIN}/>' if plates else "")
    return (f'<path d="M30 14 L98 14 L102 114 L72 114 L64 50 L56 114 L26 114 Z" fill="{m}" {OUTLINE}/>'
            f'<rect x="28" y="10" width="72" height="14" rx="4" fill="{t}" {OUTLINE}/>{pl}'
            f'<path d="M40 34 L38 100" stroke="#ffffff55" stroke-width="4" stroke-linecap="round"/>')


# ---------- jewellery ----------
def ring():
    gold = g(lin(GOLD, 0, 0, 1, 1))
    gem = g(rad([(0, "#ffffff"), (0.4, "#8fe6ff"), (1, "#1c6fd0")], 0.4, 0.35, 0.7))
    return (f'<ellipse cx="64" cy="78" rx="34" ry="30" fill="none" stroke="{INK}" stroke-width="17"/>'
            f'<ellipse cx="64" cy="78" rx="34" ry="30" fill="none" stroke="{gold}" stroke-width="11"/>'
            f'<path d="M36 66 C42 56 52 52 60 51" stroke="#ffffffaa" stroke-width="3" fill="none" stroke-linecap="round"/>'
            f'<path d="M64 14 L84 34 L64 56 L44 34 Z" fill="{gem}" {OUTLINE}/>'
            f'<path d="M44 34 L84 34 M64 14 L56 34 L64 56 L72 34 Z" stroke="{INK}" stroke-width="1.5" fill="none" opacity="0.7"/>')


def amulet():
    gold = g(lin(GOLD, 0, 0, 1, 1))
    gem = g(rad([(0, "#fff6c0"), (0.45, "#ffb428"), (1, "#b65a06")], 0.4, 0.35, 0.7))
    return (f'<path d="M20 14 C24 52 44 66 64 70 C84 66 104 52 108 14" fill="none" stroke="{INK}" stroke-width="9" stroke-linecap="round"/>'
            f'<path d="M20 14 C24 52 44 66 64 70 C84 66 104 52 108 14" fill="none" stroke="{gold}" stroke-width="4.5" stroke-linecap="round" stroke-dasharray="7 4"/>'
            f'<path d="M64 62 L88 88 L64 118 L40 88 Z" fill="{gold}" {OUTLINE}/>'
            f'<ellipse cx="64" cy="89" rx="13" ry="17" fill="{gem}" {THIN}/>'
            f'<ellipse cx="59" cy="82" rx="4" ry="6" fill="#ffffffcc"/>')


# ---------- consumables ----------
def potion(liquid, round_flask=True, sparkle=None):
    l1, l2, l3 = liquid
    glass = "#dff4ff55"
    liq = g(lin([(0, l1), (0.6, l2), (1, l3)]))
    cork = g(lin(WOOD))
    if round_flask:
        body = "M52 40 L52 56 C30 64 22 84 30 100 C38 116 90 116 98 100 C106 84 98 64 76 56 L76 40 Z"
        fill = "M31 84 C30 90 30 96 31 100 C39 114 89 114 97 100 C98 96 98 90 97 84 C80 78 48 90 31 84 Z"
    else:
        body = "M52 40 L52 58 L28 104 C26 112 32 116 38 116 L90 116 C96 116 102 112 100 104 L76 58 L76 40 Z"
        fill = "M38 86 L30 104 C29 110 33 114 38 114 L90 114 C95 114 99 110 98 104 L90 86 C74 80 54 92 38 86 Z"
    sp = (f'<path d="M64 84 l3 7 7 3 -7 3 -3 7 -3 -7 -7 -3 7 -3 Z" fill="{sparkle}" opacity="0.9"/>' if sparkle else "")
    return (f'<path d="{body}" fill="{glass}" {OUTLINE}/>'
            f'<path d="{fill}" fill="{liq}"/>{sp}'
            f'<path d="{body}" fill="none" {OUTLINE}/>'
            f'<rect x="48" y="32" width="32" height="10" rx="3" fill="{g(lin(GOLD))}" {OUTLINE}/>'
            f'<rect x="54" y="14" width="20" height="20" rx="4" fill="{cork}" {OUTLINE}/>'
            f'<path d="M40 74 C38 84 38 92 42 100" stroke="#ffffffaa" stroke-width="4" fill="none" stroke-linecap="round"/>')


# ---------- materials, loot, quest ----------
def crystal():
    c = g(lin([(0, "#ffd0dc"), (0.45, "#ff4a6e"), (1, "#8a0f2e")], 0, 0, 1, 1))
    return (f'<path d="M58 10 L84 40 L74 112 L46 104 L34 52 Z" fill="{c}" {OUTLINE}/>'
            f'<path d="M58 10 L62 54 L34 52 M62 54 L84 40 M62 54 L74 112 M62 54 L46 104" stroke="{INK}" stroke-width="2" fill="none" opacity="0.75"/>'
            f'<path d="M58 10 L62 54 L34 52 Z" fill="#ffffff55"/>'
            f'<path d="M90 70 L104 86 L98 114 L84 108 Z" fill="{c}" {OUTLINE}/>'
            f'<path d="M22 84 L34 78 L38 108 L26 104 Z" fill="{c}" {OUTLINE}/>')


def ore():
    rock = g(lin([(0, "#9a8f84"), (0.5, "#6b625a"), (1, "#3b3632")], 0, 0, 1, 1))
    iron = g(lin(STEEL, 0, 0, 1, 1))
    return (f'<path d="M20 88 L34 50 L62 36 L96 46 L112 82 L92 110 L44 112 Z" fill="{rock}" {OUTLINE}/>'
            f'<path d="M34 50 L58 66 L62 36 M58 66 L96 46 M58 66 L70 110 M58 66 L20 88" stroke="{INK}" stroke-width="2" fill="none" opacity="0.6"/>'
            f'<path d="M40 60 L52 56 L56 68 L44 74 Z" fill="{iron}" {THIN}/>'
            f'<path d="M72 54 L88 56 L86 72 L72 70 Z" fill="{iron}" {THIN}/>'
            f'<path d="M64 84 L80 82 L84 98 L68 100 Z" fill="{iron}" {THIN}/>'
            f'<path d="M30 86 L42 82 L44 96 L32 98 Z" fill="{iron}" {THIN}/>')


def cube_fragment():
    top = g(lin([(0, "#fff0a6"), (1, "#f0c040")], 0, 0, 1, 1))
    left = g(lin([(0, "#e0a92a"), (1, "#a8741a")]))
    right = g(lin([(0, "#c08a1e"), (1, "#7a5210")]))
    return (f'<path d="M64 18 L104 38 L64 60 L24 38 Z" fill="{top}" {OUTLINE}/>'
            f'<path d="M24 38 L64 60 L64 82 L50 92 L40 78 L24 84 Z" fill="{left}" {OUTLINE}/>'
            f'<path d="M104 38 L64 60 L64 82 L78 96 L90 80 L104 88 Z" fill="{right}" {OUTLINE}/>'
            f'<path d="M36 104 L48 98 L54 110 L42 116 Z" fill="{left}" {OUTLINE}/><path d="M76 106 L88 102 L92 114 L80 116 Z" fill="{right}" {OUTLINE}/>')


def letter():
    paper = g(lin([(0, "#fff7e0"), (1, "#d9c79a")], 0, 0, 1, 1))
    return (f'<rect x="16" y="32" width="96" height="68" rx="5" fill="{paper}" {OUTLINE}/>'
            f'<path d="M16 36 L64 74 L112 36" fill="none" {OUTLINE}/>'
            f'<path d="M16 98 L50 66 M112 98 L78 66" stroke="{INK}" stroke-width="2" opacity="0.5"/>'
            f'<circle cx="64" cy="76" r="14" fill="{g(rad([(0, "#ff8a7a"), (1, "#a01818")], 0.4, 0.35, 0.7))}" {OUTLINE}/>'
            f'<path d="M57 76 l5 5 9 -10" stroke="#ffe0d8" stroke-width="3.5" fill="none" stroke-linecap="round" stroke-linejoin="round"/>')


def wolf_core():
    orb = g(rad([(0, "#f4fff0"), (0.35, "#9be37a"), (1, "#1f6a2c")], 0.38, 0.34, 0.75))
    return (f'<circle cx="64" cy="66" r="42" fill="{orb}" {OUTLINE}/>'
            f'<path d="M48 44 L58 78 L64 60 L70 78 L80 44 L72 58 L64 46 L56 58 Z" fill="#f5f1e6" {OUTLINE}/>'
            f'<ellipse cx="48" cy="46" rx="9" ry="5" fill="#ffffff99" transform="rotate(-30 48 46)"/>')


def wolf_soul():
    w = g(lin([(0, "#f2fdff"), (0.5, "#8fd8ff"), (1, "#2b62c8")]))
    return (f'<path d="M64 12 C92 20 104 46 96 70 C92 84 100 96 110 104 C92 112 76 106 70 92 C64 108 46 116 28 108 C40 100 44 88 38 72 C28 46 40 18 64 12 Z" fill="{w}" {OUTLINE}/>'
            f'<ellipse cx="52" cy="52" rx="6" ry="9" fill="{INK}"/><ellipse cx="78" cy="52" rx="6" ry="9" fill="{INK}"/>'
            f'<path d="M56 74 C60 80 68 80 72 74" stroke="{INK}" stroke-width="3" fill="none" stroke-linecap="round"/>'
            f'<path d="M44 30 C50 22 58 19 64 18" stroke="#ffffffcc" stroke-width="4" fill="none" stroke-linecap="round"/>')


def golem_heart():
    stone = g(lin([(0, "#a3a8b3"), (0.5, "#6c717d"), (1, "#383c45")], 0, 0, 1, 1))
    return (f'<path d="M64 112 C24 84 12 58 22 38 C30 20 54 18 64 38 C74 18 98 20 106 38 C116 58 104 84 64 112 Z" fill="{stone}" {OUTLINE}/>'
            f'<path d="M64 38 L56 58 L70 68 L58 88 L64 112" stroke="#ffb347" stroke-width="6" fill="none" stroke-linejoin="round" stroke-linecap="round"/>'
            f'<path d="M64 38 L56 58 L70 68 L58 88 L64 112" stroke="#fff3c4" stroke-width="2" fill="none" stroke-linejoin="round" stroke-linecap="round"/>'
            f'<path d="M56 58 L36 54 M70 68 L92 62 M58 88 L42 86" stroke="#ffb347" stroke-width="4" stroke-linecap="round"/>'
            f'<path d="M30 40 C34 30 42 27 48 28" stroke="#ffffff77" stroke-width="4" fill="none" stroke-linecap="round"/>')


def coin():
    gold = g(rad([(0, "#fff8c8"), (0.5, "#f2c13a"), (1, "#a66a10")], 0.38, 0.32, 0.8))
    return (f'<ellipse cx="64" cy="70" rx="46" ry="44" fill="#7a4c0a" {OUTLINE}/>'
            f'<circle cx="64" cy="62" r="44" fill="{gold}" {OUTLINE}/>'
            f'<circle cx="64" cy="62" r="33" fill="none" stroke="#a66a10" stroke-width="3"/>'
            f'<path d="M64 40 L70 55 L86 57 L74 68 L78 84 L64 75 L50 84 L54 68 L42 57 L58 55 Z" fill="#fff3b0" {THIN}/>')


# ---------- skills ----------
def fireball():
    f1 = g(rad([(0, "#fffbe0"), (0.35, "#ffd23a"), (0.7, "#ff6a1a"), (1, "#b01c00")], 0.42, 0.55, 0.7))
    return (f'<path d="M108 14 C92 28 100 40 86 44 C96 30 84 22 70 30 C82 30 82 42 70 44 C34 46 16 70 22 92 C28 114 56 122 78 110 C102 96 104 70 92 56 C104 52 110 34 108 14 Z" fill="{f1}" {OUTLINE}/>'
            f'<circle cx="54" cy="84" r="20" fill="#fff6c8"/><circle cx="50" cy="80" r="9" fill="#ffffff"/>')


def mend():
    c = g(lin([(0, "#ffffff"), (0.5, "#b8e8ff"), (1, "#6a8cff")]))
    return (f'<path d="M52 20 h24 v32 h32 v24 h-32 v32 h-24 v-32 h-32 v-24 h32 Z" fill="{c}" {OUTLINE}/>'
            f'<path d="M58 28 v30 h-30" stroke="#ffffffcc" stroke-width="4" fill="none" stroke-linecap="round"/>'
            f'<path d="M100 18 l3 8 8 3 -8 3 -3 8 -3 -8 -8 -3 8 -3 Z M24 92 l2.5 6 6 2.5 -6 2.5 -2.5 6 -2.5 -6 -6 -2.5 6 -2.5 Z M104 96 l2 5 5 2 -5 2 -2 5 -2 -5 -5 -2 5 -2 Z" fill="#fff6a8" {THIN}/>')


def nova():
    core = g(rad([(0, "#ffffff"), (0.4, "#d9c2ff"), (1, "#6a35e0")]))
    rays = "".join(f'<path d="M64 64 L{64 + 58 * c:.1f} {64 + 58 * s:.1f}" stroke="#e7d6ff" stroke-width="6" stroke-linecap="round"/>'
                   for c, s in [(1, 0), (-1, 0), (0, 1), (0, -1), (0.707, 0.707), (-0.707, 0.707), (0.707, -0.707), (-0.707, -0.707)])
    return (f'<circle cx="64" cy="64" r="50" fill="none" stroke="#b08cff" stroke-width="5" stroke-dasharray="14 9"/>'
            f'{rays}<circle cx="64" cy="64" r="26" fill="{core}" {OUTLINE}/><circle cx="56" cy="56" r="8" fill="#ffffff"/>')


def strike():
    return (f'<path d="M14 100 C40 96 84 66 112 14 C104 70 62 108 14 100 Z" fill="{g(lin([(0, "#ffffff"), (1, "#ffd27a")], 0, 1, 1, 0))}" {OUTLINE}/>'
            + f'<g transform="translate(6 6) scale(0.9)">{sword()}</g>')


def blood():
    drop = g(rad([(0, "#ff9a9a"), (0.45, "#e01c2c"), (1, "#6a0410")], 0.4, 0.4, 0.75))
    return (f'<path d="M18 30 C50 34 86 62 110 108 C80 78 46 56 18 30 Z" fill="#ffd9d9" {OUTLINE}/>'
            f'<path d="M64 20 C84 50 96 66 96 84 C96 104 82 116 64 116 C46 116 32 104 32 84 C32 66 44 50 64 20 Z" fill="{drop}" {OUTLINE}/>'
            f'<path d="M48 76 C46 88 50 98 58 104" stroke="#ffffffaa" stroke-width="5" fill="none" stroke-linecap="round"/>')


def wind():
    return (f'<path d="M20 44 C50 20 96 30 96 54 C96 70 74 72 70 60" fill="none" stroke="{INK}" stroke-width="13" stroke-linecap="round"/>'
            f'<path d="M20 44 C50 20 96 30 96 54 C96 70 74 72 70 60" fill="none" stroke="#c9ffd6" stroke-width="7" stroke-linecap="round"/>'
            f'<path d="M14 72 C50 60 108 66 108 88 C108 104 84 104 82 92" fill="none" stroke="{INK}" stroke-width="13" stroke-linecap="round"/>'
            f'<path d="M14 72 C50 60 108 66 108 88 C108 104 84 104 82 92" fill="none" stroke="#7df09a" stroke-width="7" stroke-linecap="round"/>'
            f'<path d="M26 100 C46 94 66 96 70 106" fill="none" stroke="{INK}" stroke-width="11" stroke-linecap="round"/>'
            f'<path d="M26 100 C46 94 66 96 70 106" fill="none" stroke="#e8fff0" stroke-width="5" stroke-linecap="round"/>')


def bite():
    gum = g(lin([(0, "#c2343c"), (1, "#5c0d14")]))
    tooth = "#fbf6e8"
    upper = "".join(f'<path d="M{x} 44 L{x + 9} 44 L{x + 4.5} {72 if i in (1, 4) else 62} Z" fill="{tooth}" {THIN}/>' for i, x in enumerate([24, 38, 52, 67, 81, 95]))
    lower = "".join(f'<path d="M{x} 92 L{x + 9} 92 L{x + 4.5} {64 if i in (1, 3) else 74} Z" fill="{tooth}" {THIN}/>' for i, x in enumerate([31, 45, 59.5, 74, 88]))
    return (f'<path d="M14 20 C40 8 88 8 114 20 L108 46 L20 46 Z" fill="{gum}" {OUTLINE}/>'
            f'<path d="M20 90 L108 90 L114 108 C88 120 40 120 14 108 Z" fill="{gum}" {OUTLINE}/>{upper}{lower}')


def slam():
    fist = g(lin([(0, "#c4c9d2"), (0.5, "#8c929d"), (1, "#4a4f59")], 0, 0, 1, 1))
    cracks = "".join(f'<path d="M64 102 L{x} {y}" stroke="#ffcf6a" stroke-width="4" stroke-linecap="round"/>' for x, y in [(14, 96), (30, 118), (98, 118), (114, 96), (64, 124)])
    return (f'{cracks}<path d="M36 30 C36 20 46 20 48 28 C48 16 60 16 62 26 C62 14 76 14 78 26 C80 18 92 20 92 30 L94 70 C94 86 82 96 64 96 C46 96 34 86 34 70 Z" fill="{fist}" {OUTLINE}/>'
            f'<path d="M48 28 L48 52 M62 26 L62 52 M78 26 L78 52" stroke="{INK}" stroke-width="2.5"/>'
            f'<path d="M34 60 C50 54 78 54 94 60" stroke="{INK}" stroke-width="2.5" fill="none"/>'
            f'<path d="M42 36 L42 50" stroke="#ffffff88" stroke-width="4" stroke-linecap="round"/>')


def smash():
    rock = g(lin([(0, "#b3ada5"), (0.5, "#7b746c"), (1, "#3f3a35")], 0, 0, 1, 1))
    return (f'<path d="M40 12 L84 8 L108 36 L98 70 L62 82 L28 64 L22 34 Z" fill="{rock}" {OUTLINE}/>'
            f'<path d="M40 12 L58 40 L84 8 M58 40 L98 70 M58 40 L28 64 M58 40 L62 82" stroke="{INK}" stroke-width="2" fill="none" opacity="0.6"/>'
            f'<path d="M10 104 L34 96 L48 108 L62 92 L78 108 L94 94 L118 104" fill="none" stroke="#ffcf6a" stroke-width="6" stroke-linejoin="round" stroke-linecap="round"/>'
            f'<path d="M20 120 L30 108 M64 124 L64 106 M108 120 L98 108" stroke="#ffcf6a" stroke-width="4" stroke-linecap="round"/>')


# ---------- statuses ----------
def flame():
    f1 = g(lin([(0, "#fff6b0"), (0.4, "#ffb21e"), (1, "#d42a00")], 0, 1, 0, 0))
    return (f'<path d="M64 10 C70 34 98 46 98 78 C98 102 84 118 64 118 C44 118 30 102 30 78 C30 62 40 54 44 40 C50 52 56 54 58 44 C60 34 60 22 64 10 Z" fill="{f1}" {OUTLINE}/>'
            f'<path d="M64 60 C68 74 82 80 80 96 C78 108 72 114 64 114 C56 114 48 108 48 96 C48 86 56 82 58 72 C60 76 62 70 64 60 Z" fill="#fff3c0"/>')


def stun():
    stars = "".join(f'<path d="M{x} {y} l4 9 10 1 -8 7 3 10 -9 -6 -9 6 3 -10 -8 -7 10 -1 Z" fill="#ffe46a" {THIN}/>' for x, y in [(30, 22), (88, 18), (104, 62)])
    return (f'<path d="M64 64 m-40 0 a40 40 0 1 1 80 0 a30 30 0 1 1 -60 0 a20 20 0 1 1 40 0 a10 10 0 1 1 -20 0" fill="none" stroke="{INK}" stroke-width="11" stroke-linecap="round"/>'
            f'<path d="M64 64 m-40 0 a40 40 0 1 1 80 0 a30 30 0 1 1 -60 0 a20 20 0 1 1 40 0 a10 10 0 1 1 -20 0" fill="none" stroke="#f4e9ff" stroke-width="5" stroke-linecap="round"/>{stars}')


def sunder():
    return (shield(IRON, IRON, "")
            + f'<path d="M70 12 L56 44 L74 58 L54 84 L66 118" stroke="{INK}" stroke-width="10" fill="none" stroke-linejoin="round"/>'
            + f'<path d="M70 12 L56 44 L74 58 L54 84 L66 118" stroke="#ff5a4a" stroke-width="4" fill="none" stroke-linejoin="round"/>')


def ward():
    bubble = g(rad([(0, "#ffffff22"), (0.7, "#7fd4ff55"), (1, "#3aa0ffcc")]))
    return (f'<circle cx="64" cy="64" r="50" fill="{bubble}" stroke="#bfeaff" stroke-width="4"/>'
            f'<path d="M64 26 L92 38 C92 66 84 86 64 100 C44 86 36 66 36 38 Z" fill="{g(lin(BLUESTEEL, 0, 0, 1, 1))}" {OUTLINE}/>'
            f'<path d="M64 44 L64 82 M48 60 L80 60" stroke="#ffffff" stroke-width="6" stroke-linecap="round"/>'
            f'<path d="M30 40 C36 28 46 22 56 20" stroke="#ffffffcc" stroke-width="4" fill="none" stroke-linecap="round"/>')


def regen():
    leaf = g(lin([(0, "#d8ffd0"), (0.5, "#5ad86a"), (1, "#167a2c")], 0, 0, 1, 1))
    return (f'<path d="M64 112 C28 88 16 62 26 42 C34 26 56 26 64 44 C72 26 94 26 102 42 C112 62 100 88 64 112 Z" fill="{leaf}" {OUTLINE}/>'
            f'<path d="M56 50 h16 v14 h14 v16 h-14 v14 h-16 v-14 h-14 v-16 h14 Z" fill="#ffffff" {THIN}/>'
            f'<path d="M34 46 C38 38 46 35 52 36" stroke="#ffffffaa" stroke-width="4" fill="none" stroke-linecap="round"/>')


def iron_skin():
    return (shield(STEEL, IRON, "")
            + f'<path d="M44 46 L84 46 M40 62 L88 62 M46 78 L82 78" stroke="{INK}" stroke-width="3" opacity="0.7"/>'
            + "".join(f'<circle cx="{x}" cy="{y}" r="3.5" fill="#f4f7fb" {THIN}/>' for x, y in [(44, 38), (84, 38), (64, 96), (40, 70), (88, 70)]))


def fury():
    f1 = g(lin([(0, "#ffd0a0"), (0.4, "#ff4a2a"), (1, "#8a0c0c")], 0, 1, 0, 0))
    return (f'<path d="M64 8 C74 30 104 44 102 78 C100 104 84 120 64 120 C44 120 28 104 26 78 C24 60 36 52 40 36 C48 48 54 48 56 38 C58 28 60 18 64 8 Z" fill="{f1}" {OUTLINE}/>'
            f'<path d="M38 66 L58 76 L58 84 L38 80 Z M90 66 L70 76 L70 84 L90 80 Z" fill="#fff3c0" {THIN}/>'
            f'<path d="M46 100 L54 92 L60 100 L64 92 L68 100 L74 92 L82 100" stroke="#fff3c0" stroke-width="4" fill="none" stroke-linejoin="round" stroke-linecap="round"/>')


# ---------- window buttons (light glyph on blue) ----------
LIGHT = "#e9f1ff"
W = f'fill="{LIGHT}" {OUTLINE}'
WL = f'fill="none" stroke="{LIGHT}" stroke-width="7" stroke-linecap="round" stroke-linejoin="round"'


def w_character():
    return f'<circle cx="64" cy="40" r="22" {W}/><path d="M22 114 C22 82 42 68 64 68 C86 68 106 82 106 114 Z" {W}/>'


def w_backpack():
    lb = g(lin(LEATHER, 0, 0, 1, 1))
    return (f'<path d="M46 26 C46 8 82 8 82 26" fill="none" stroke="{INK}" stroke-width="11" stroke-linecap="round"/><path d="M46 26 C46 8 82 8 82 26" fill="none" stroke="#c99a6a" stroke-width="5" stroke-linecap="round"/>'
            f'<path d="M30 40 C30 26 98 26 98 40 L104 104 C104 114 24 114 24 104 Z" fill="{lb}" {OUTLINE}/>'
            f'<path d="M30 40 C40 62 88 62 98 40" fill="#00000022" {THIN}/>'
            f'<rect x="42" y="72" width="44" height="30" rx="6" fill="{g(lin(WOOD))}" {OUTLINE}/>'
            f'<rect x="58" y="50" width="12" height="20" rx="3" fill="{g(lin(GOLD))}" {OUTLINE}/>')


def w_talents():
    return f'<path d="M64 10 L78 46 L116 48 L86 72 L96 110 L64 88 L32 110 L42 72 L12 48 L50 46 Z" fill="{g(lin(GOLD))}" {OUTLINE}/><path d="M64 26 L72 50" stroke="#ffffffaa" stroke-width="4" stroke-linecap="round"/>'


def w_map():
    paper = g(lin([(0, "#fff3d0"), (1, "#c9ad74")], 0, 0, 1, 1))
    return (f'<path d="M14 28 L46 16 L82 28 L114 16 L114 100 L82 112 L46 100 L14 112 Z" fill="{paper}" {OUTLINE}/>'
            f'<path d="M46 16 L46 100 M82 28 L82 112" stroke="{INK}" stroke-width="2.5" opacity="0.5"/>'
            f'<path d="M26 84 C36 70 44 80 54 66 S74 52 88 60" fill="none" stroke="#b03a2a" stroke-width="4" stroke-dasharray="6 5" stroke-linecap="round"/>'
            f'<path d="M92 44 l8 8 M100 44 l-8 8" stroke="#b03a2a" stroke-width="4" stroke-linecap="round"/>')


def w_book():
    cover = g(lin([(0, "#7a5fd0"), (1, "#3a2a80")], 0, 0, 1, 1))
    return (f'<path d="M20 20 L60 26 L60 112 L20 104 Z M108 20 L68 26 L68 112 L108 104 Z" fill="{cover}" {OUTLINE}/>'
            f'<path d="M26 28 L56 33 L56 104 L26 98 Z M102 28 L72 33 L72 104 L102 98 Z" fill="#fff7e0" {THIN}/>'
            f'<path d="M32 44 L50 47 M32 56 L50 59 M32 68 L50 71 M96 44 L78 47 M96 56 L78 59 M96 68 L78 71" stroke="{INK}" stroke-width="2.5" opacity="0.55" stroke-linecap="round"/>'
            f'<path d="M84 84 l3 7 7 3 -7 3 -3 7 -3 -7 -7 -3 7 -3 Z" fill="#f0c040" {THIN}/>')


def w_log():
    paper = g(lin([(0, "#fff7e0"), (1, "#d9c79a")], 0, 0, 1, 1))
    return (f'<rect x="26" y="14" width="76" height="100" rx="6" fill="{paper}" {OUTLINE}/>'
            f'<path d="M38 36 h52 M38 52 h52 M38 68 h34 M38 84 h44" stroke="{INK}" stroke-width="4" stroke-linecap="round" opacity="0.6"/>'
            f'<path d="M38 36 h18 M38 68 h12" stroke="#c0392b" stroke-width="4" stroke-linecap="round"/>'
            f'<path d="M38 52 h26 M38 84 h20" stroke="#27ae60" stroke-width="4" stroke-linecap="round"/>')


def w_group():
    return (f'<circle cx="34" cy="50" r="15" {W}/><path d="M8 106 C8 82 22 72 34 72 C46 72 56 80 58 94 L58 106 Z" {W}/>'
            f'<circle cx="94" cy="50" r="15" {W}/><path d="M120 106 C120 82 106 72 94 72 C82 72 72 80 70 94 L70 106 Z" {W}/>'
            f'<circle cx="64" cy="40" r="18" fill="#ffffff" {OUTLINE}/><path d="M30 114 C30 86 46 70 64 70 C82 70 98 86 98 114 Z" fill="#ffffff" {OUTLINE}/>')


def w_friends():
    heart = g(lin([(0, "#ff9aa8"), (1, "#d81e3c")]))
    return (f'<circle cx="44" cy="44" r="18" {W}/><path d="M12 112 C12 84 28 72 44 72 C60 72 76 84 76 112 Z" {W}/>'
            f'<path d="M90 110 C66 92 62 76 68 66 C74 56 86 58 90 68 C94 58 106 56 112 66 C118 76 114 92 90 110 Z" fill="{heart}" {OUTLINE}/>')


def w_settings():
    teeth = "".join(f'<rect x="56" y="6" width="16" height="24" rx="4" fill="{LIGHT}" {OUTLINE} transform="rotate({a} 64 64)"/>' for a in range(0, 360, 45))
    return f'{teeth}<circle cx="64" cy="64" r="38" {W}/><circle cx="64" cy="64" r="15" fill="#0e1626" {OUTLINE}/>'


def w_anvil():
    m = g(lin(IRON, 0, 0, 0, 1))
    return (f'<path d="M12 36 L92 36 C100 36 110 30 116 26 C116 44 104 56 86 58 L86 70 C92 74 96 82 98 92 L30 92 C32 82 36 74 42 70 L42 56 C28 54 16 46 12 36 Z" fill="{m}" {OUTLINE}/>'
            f'<rect x="22" y="92" width="84" height="18" rx="4" fill="{m}" {OUTLINE}/>'
            f'<path d="M20 40 L88 40" stroke="#ffffffaa" stroke-width="3" stroke-linecap="round"/>')


def w_whetstone():
    stone = g(rad([(0, "#eef1f5"), (0.6, "#9aa2ad"), (1, "#525964")], 0.4, 0.38, 0.75))
    return (f'<path d="M30 116 L46 70 M98 116 L82 70" stroke="{INK}" stroke-width="12" stroke-linecap="round"/><path d="M30 116 L46 70 M98 116 L82 70" stroke="#8d5a2e" stroke-width="6" stroke-linecap="round"/>'
            f'<circle cx="64" cy="54" r="42" fill="{stone}" {OUTLINE}/><circle cx="64" cy="54" r="27" fill="none" stroke="{INK}" stroke-width="2" opacity="0.4"/>'
            f'<circle cx="64" cy="54" r="8" fill="{g(lin(GOLD))}" {OUTLINE}/>'
            f'<path d="M100 22 l5 -9 M108 30 l10 -4 M104 14 l2 2" stroke="#ffd27a" stroke-width="4" stroke-linecap="round"/>')


def w_trade():
    return (f'<path d="M14 44 L84 44 L84 26 L116 52 L84 78 L84 60 L14 60 Z" fill="#7df09a" {OUTLINE} transform="translate(0 -8)"/>'
            f'<path d="M114 84 L44 84 L44 66 L12 92 L44 118 L44 100 L114 100 Z" fill="#ffd27a" {OUTLINE} transform="translate(0 -4)"/>')


def w_chat():
    return (f'<path d="M14 22 C14 14 20 10 28 10 L100 10 C108 10 114 14 114 22 L114 72 C114 80 108 84 100 84 L60 84 L34 110 L38 84 L28 84 C20 84 14 80 14 72 Z" {W}/>'
            f'<path d="M34 34 h60 M34 50 h60 M34 66 h36" stroke="{INK}" stroke-width="5" stroke-linecap="round" opacity="0.55"/>')


def w_market():
    return (f'<path d="M10 46 L22 16 L106 16 L118 46 Z" fill="#e0483c" {OUTLINE}/>'
            f'<path d="M38 16 L32 46 M54 16 L52 46 M74 16 L76 46 M90 16 L96 46" stroke="#fff3e6" stroke-width="7"/>'
            f'<path d="M10 46 C10 60 30 60 32 46 C34 60 52 60 54 46 C56 60 74 60 76 46 C78 60 96 60 98 46 C100 60 118 60 118 46 Z" fill="#fff3e6" {OUTLINE}/>'
            f'<rect x="20" y="62" width="88" height="50" rx="4" fill="{g(lin(WOOD))}" {OUTLINE}/>'
            f'<circle cx="46" cy="86" r="12" fill="{g(lin(GOLD))}" {OUTLINE}/><rect x="68" y="74" width="28" height="26" rx="3" fill="#7fd4ff" {OUTLINE}/>')


def w_shop():
    bag = g(lin(LEATHER, 0, 0, 1, 1))
    return (f'<path d="M44 34 C36 22 44 12 52 18 C58 10 70 10 76 18 C84 12 92 22 84 34 Z" fill="{bag}" {OUTLINE}/>'
            f'<path d="M44 34 L84 34 C108 58 114 90 100 108 C90 118 38 118 28 108 C14 90 20 58 44 34 Z" fill="{bag}" {OUTLINE}/>'
            f'<rect x="42" y="30" width="44" height="9" rx="4" fill="{g(lin(GOLD))}" {OUTLINE}/>'
            f'<circle cx="64" cy="78" r="20" fill="{g(lin(GOLD))}" {OUTLINE}/>'
            f'<path d="M64 64 L68 74 L79 75 L71 82 L74 93 L64 87 L54 93 L57 82 L49 75 L60 74 Z" fill="#fff3b0" {THIN}/>')


def w_mail():
    paper = g(lin([(0, "#ffffff"), (1, "#cfdcf0")], 0, 0, 1, 1))
    return (f'<rect x="12" y="28" width="104" height="74" rx="6" fill="{paper}" {OUTLINE}/>'
            f'<path d="M12 34 L64 74 L116 34" fill="none" {OUTLINE}/>'
            f'<path d="M12 98 L48 64 M116 98 L80 64" stroke="{INK}" stroke-width="2.5" opacity="0.5"/>')


def w_orders():
    paper = g(lin([(0, "#fff7e0"), (1, "#d9c79a")], 0, 0, 1, 1))
    return (f'<rect x="14" y="20" width="66" height="92" rx="5" fill="{paper}" {OUTLINE}/>'
            f'<path d="M26 40 h42 M26 56 h42 M26 72 h28" stroke="{INK}" stroke-width="4" stroke-linecap="round" opacity="0.55"/>'
            f'<g transform="rotate(35 86 70)"><rect x="80" y="44" width="12" height="70" rx="4" fill="{g(lin(WOOD, 0, 0, 1, 0))}" {OUTLINE}/>'
            f'<rect x="62" y="24" width="48" height="26" rx="5" fill="{g(lin(IRON))}" {OUTLINE}/></g>')


# ---------- emotes ----------
def e_taunt():
    face = g(rad([(0, "#fff3a0"), (0.6, "#f6c632"), (1, "#c98a10")], 0.4, 0.35, 0.8))
    tongue = g(lin([(0, "#ff8a9a"), (1, "#d8304a")]))
    return (f'<circle cx="64" cy="64" r="50" fill="{face}" {OUTLINE}/>'
            f'<path d="M30 40 L54 46" stroke="{INK}" stroke-width="6" stroke-linecap="round"/>'
            f'<path d="M74 38 C82 30 94 30 100 38" stroke="{INK}" stroke-width="6" fill="none" stroke-linecap="round"/>'
            f'<path d="M34 58 C40 52 48 52 54 58" stroke="{INK}" stroke-width="6" fill="none" stroke-linecap="round"/>'
            f'<ellipse cx="86" cy="54" rx="9" ry="11" fill="#ffffff" {THIN}/><circle cx="88" cy="56" r="5" fill="{INK}"/>'
            f'<path d="M36 78 C50 92 78 92 94 74" stroke="{INK}" stroke-width="6" fill="none" stroke-linecap="round"/>'
            f'<path d="M56 86 C56 110 82 110 80 84 Z" fill="{tongue}" {OUTLINE}/>'
            f'<path d="M68 90 L68 102" stroke="{INK}" stroke-width="2.5" opacity="0.6" stroke-linecap="round"/>'
            f'<path d="M30 50 C24 60 24 72 30 82" stroke="#ffffff88" stroke-width="4" fill="none" stroke-linecap="round"/>')


def frame(bg, shapes, glow=None):
    """Icon factory: collects the gradients the shape functions registered (shapes is a callable)."""
    def build():
        DEFS.clear()
        body = shapes()
        return icon(bg, body, glow)
    return build


SWORDSMAN = BLUESTEEL
ICONS = {
    "items": {
        "apprentice_staff": frame(BG_WEAPON, staff, "#4aa8ff"),
        "iron_sword": frame(BG_WEAPON, lambda: sword(STEEL, IRON, LEATHER)),
        "swordsman_sword": frame(BG_WEAPON, lambda: sword(SWORDSMAN, GOLD, LEATHER, "#ff4a6e"), "#5a8cff"),
        "wooden_shield": frame(BG_WEAPON, lambda: shield(WOOD, IRON, emblem_boss())),
        "swordsman_shield": frame(BG_WEAPON, lambda: shield(SWORDSMAN, GOLD, emblem_lion()), "#5a8cff"),
        "great_axe": frame(BG_WEAPON, axe, "#ffb347"),
        "cloth_hood": frame(BG_CLOTH, hood),
        "cloth_robe": frame(BG_CLOTH, robe),
        "adept_gloves": frame(BG_CLOTH, lambda: glove(CLOTH2, GOLD)),
        "adept_trousers": frame(BG_CLOTH, lambda: legs(CLOTH2, GOLD)),
        "adept_boots": frame(BG_CLOTH, lambda: boot(CLOTH2, LEATHER)),
        "iron_helm": frame(BG_ARMOR, lambda: helm(IRON, IRON)),
        "plate_armor": frame(BG_ARMOR, lambda: breastplate(IRON, STEEL)),
        "leather_gloves": frame(BG_ARMOR, lambda: glove(LEATHER, WOOD)),
        "swordsman_helm": frame(BG_ARMOR, lambda: helm(SWORDSMAN, GOLD, "#d8303c"), "#5a8cff"),
        "swordsman_armor": frame(BG_ARMOR, lambda: breastplate(SWORDSMAN, GOLD), "#5a8cff"),
        "swordsman_gauntlets": frame(BG_ARMOR, lambda: glove(SWORDSMAN, GOLD, True), "#5a8cff"),
        "swordsman_greaves": frame(BG_ARMOR, lambda: legs(SWORDSMAN, GOLD, True), "#5a8cff"),
        "swordsman_boots": frame(BG_ARMOR, lambda: boot(SWORDSMAN, GOLD, True), "#5a8cff"),
        "ring_of_focus": frame(BG_JEWEL, ring, "#4ac8ff"),
        "amber_amulet": frame(BG_JEWEL, amulet, "#ffb428"),
        "health_potion": frame(BG_POTION, lambda: potion(("#ff9a9a", "#e0222e", "#7a0812")), "#ff4a4a"),
        "main_mana_potion": frame(BG_POTION, lambda: potion(("#a8d4ff", "#2a7fe8", "#0c2f8a")), "#4a8cff"),
        "reserve_mana_potion": frame(BG_POTION, lambda: potion(("#e0b8ff", "#9a3ae0", "#4a0f8a"), True, "#ffffff"), "#b05aff"),
        "iron_skin_elixir": frame(BG_POTION, lambda: potion(("#f0f4f8", "#9aa8b8", "#4a5868"), False), "#c0d0e0"),
        "fury_elixir": frame(BG_POTION, lambda: potion(("#ffe0a0", "#ff7a1a", "#a02a00"), False, "#fff3c0"), "#ff8a2a"),
        "philosopher_stone_shard": frame(BG_MATERIAL, crystal, "#ff4a6e"),
        "iron_ore": frame(BG_MATERIAL, ore),
        "cube_fragment": frame(BG_MATERIAL, cube_fragment, "#f0c040"),
        "smith_letter": frame(BG_QUEST, letter),
        "wolf_core": frame(BG_BEAST, wolf_core, "#7df09a"),
        "wolf_soul": frame(BG_BEAST, wolf_soul, "#6ac8ff"),
        "golem_heart": frame(BG_BEAST, golem_heart, "#ffb347"),
        "gold": frame(BG_JEWEL, coin, "#f0c040"),
    },
    "skills": {
        "fireball": frame(BG_FIRE, fireball, "#ff7a1a"),
        "mend": frame(BG_ARCANE, mend, "#8ab8ff"),
        "nova": frame(BG_ARCANE, nova, "#a070ff"),
        "strike": frame(BG_STEEL, strike, "#ffd27a"),
        "blood": frame(BG_BLOOD, blood, "#ff3a3a"),
        "wind": frame(BG_NATURE, wind, "#5ad86a"),
        "bite": frame(BG_BEAST, bite),
        "slam": frame(BG_STEEL, slam, "#ffcf6a"),
        "smash": frame(BG_BEAST, smash, "#ffcf6a"),
    },
    "statuses": {
        "burning": frame(BG_DEBUFF, flame, "#ff7a1a"),
        "stunned": frame(BG_DEBUFF, stun, "#c9a8ff"),
        "sundered": frame(BG_DEBUFF, sunder),
        "ward": frame(BG_BUFF, ward, "#6ac8ff"),
        "regen": frame(BG_BUFF, regen, "#5ad86a"),
        "iron_skin": frame(BG_BUFF, iron_skin, "#c0d0e0"),
        "fury": frame(BG_BUFF, fury, "#ff5a2a"),
    },
    "emotes": {
        "taunt": frame(BG_FIRE, e_taunt, "#ffb347"),
    },
    "windows": {
        "character": frame(BG_WINDOW, w_character),
        "backpack": frame(BG_WINDOW, w_backpack),
        "talents": frame(BG_WINDOW, w_talents, "#f0c040"),
        "map": frame(BG_WINDOW, w_map),
        "skillbook": frame(BG_WINDOW, w_book),
        "combatlog": frame(BG_WINDOW, w_log),
        "group": frame(BG_WINDOW, w_group),
        "friends": frame(BG_WINDOW, w_friends),
        "settings": frame(BG_WINDOW, w_settings),
        "anvil": frame(BG_WINDOW, w_anvil),
        "whetstone": frame(BG_WINDOW, w_whetstone),
        "trade": frame(BG_WINDOW, w_trade),
        "chat": frame(BG_WINDOW, w_chat),
        "market": frame(BG_WINDOW, w_market),
        "shop": frame(BG_WINDOW, w_shop),
        "mail": frame(BG_WINDOW, w_mail),
        "orders": frame(BG_WINDOW, w_orders),
    },
}


FOLDER_META = """fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

# Import settings of an icon: a single sprite, no mipmaps, no compression (icons are small and shown in the UI).
TEXTURE_META = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 11
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMasterTextureLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 0
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 0
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 0
  alphaUsage: 1
  alphaIsTransparency: 0
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  platformSettings:
  - serializedVersion: 3
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: 
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable: {{}}
  spritePackingTag: 
  pSDRemoveMatte: 0
  pSDShowRemoveMatteOption: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def write_meta(asset_path, template):
    """Creates the Unity .meta of an asset unless it exists (an existing GUID must never change)."""
    meta = os.path.join(UNITY_PROJECT, asset_path + ".meta")
    if os.path.exists(meta):
        return
    os.makedirs(os.path.dirname(meta), exist_ok=True)
    guid = hashlib.md5(asset_path.encode("utf-8")).hexdigest()   # stable: the same path always gets the same GUID
    with open(meta, "w", encoding="utf-8", newline="\n") as f:
        f.write(template.format(guid=guid))


def main():
    count = 0
    write_meta(ASSET_ROOT, FOLDER_META)
    for group, icons in ICONS.items():
        folder = os.path.join(OUT, group)
        os.makedirs(folder, exist_ok=True)
        write_meta(f"{ASSET_ROOT}/{group}", FOLDER_META)
        for name, build in icons.items():
            with open(os.path.join(folder, name + ".svg"), "w", encoding="utf-8") as f:
                f.write(build())
            write_meta(f"{ASSET_ROOT}/{group}/{name}.png", TEXTURE_META)
            count += 1
    print(f"{count} icons written to {OUT}")


if __name__ == "__main__":
    main()
