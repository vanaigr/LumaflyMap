import list from './list.json'
import positions from './positions.json'
import { setup } from './camera.js'

setup({
    canvas: window.mapCont,
    camera: {
        posX: 800,
        posY: -500,
        scale: 500,
    },
    canvasSize: [1, 1],
    sizes: { fontSize: 16, heightCssPx: 1000 },
    requestRender() {
        const c = this.camera
        const r = window.mapResize
        const rect = window.mapCont.getBoundingClientRect()
        const height = rect.height
        const scale = 0.5 * height / c.scale
        const x = -c.posX * scale
        const y = c.posY * scale
        r.style.transform = `matrix(${scale}, 0, 0, ${scale}, ${x}, ${y})`
    },
})

console.log(list, positions)

let minX = Infinity, minY = Infinity
let maxX = -Infinity, maxY = -Infinity

/*
for(const sceneName in list) {
    const scenePosOnMap = positions.mapScenes[sceneName]
    if(scenePosOnMap == null) {
        console.log("Room not on the map " + sceneName)
        continue
    }

    minX = Math.min(minX, scenePosOnMap.origin.x - scenePosOnMap.size.x * 0.5)
    maxX = Math.max(maxX, scenePosOnMap.origin.x + scenePosOnMap.size.x * 0.5)

    minY = Math.min(minY, scenePosOnMap.origin.y - scenePosOnMap.size.y * 0.5)
    maxY = Math.max(maxY, scenePosOnMap.origin.y + scenePosOnMap.size.y * 0.5)
}
*/

const seerScene = "RestingGrounds_07";
const seerPath = "/Dream Moth/Dream Dialogue";

let currentItems = {}


function refresh() {
    console.log('refreshing')

    window.root.innerHTML = ''

    const freed = window.showFreed?.checked
    const rest = window.showRest?.checked
    const lamps = window.showLamps?.checked
    const enemies = window.showEnemies?.checked
    const beam = window.showBeam?.checked
    const chandelier = window.showChandelier?.checked
    const seer = window.showSeer?.checked

    window.rest.innerHTML = ''

    for(const sceneName in positions.objs) {
        const objs = list[sceneName] ?? {}

        const scenePosOnMap = positions.mapScenes[sceneName]
        const done = currentItems?.[sceneName] ?? {}

        let sceneRest = null

        const objsInWorld = positions.objs[sceneName]
        const sceneWorldBounds = objsInWorld.sceneBounds
        for(const path in objsInWorld.positions) {
            if(done[path] && !freed) continue
            if(!done[path] && !rest) continue

            if(!lamps && (objs.lamps?.[path] || objs.chests?.[path])) continue
            if(!enemies && objs.enemies?.[path]) continue
            if(!beam && objs.beamMiners?.[path]) continue
            if(!chandelier && (objs.chandeliers?.[path] || objs.lampsOnWalls?.[path])) continue
            if(!seer && (sceneName === seerScene && path === seerPath)) continue

            const obj = objsInWorld.positions[path]

            if(scenePosOnMap == null) {
                if(!sceneRest) {
                    const cont = document.createElement('details')
                    cont.classList.add('restScene')
                    const title = document.createElement('summary')
                    title.append(document.createTextNode(sceneName))
                    cont.append(title)
                    sceneRest = document.createElement('div')
                    cont.append(sceneRest)
                    window.rest.append(cont)
                }

                const el = document.createElement('div')
                el.append(document.createTextNode(path))

                sceneRest.append(el)
            }
            else {
                const el = document.createElement('div')
                el.append(document.createElement('div'))
                el.classList.add('marker')

                const width = scenePosOnMap.max.x - scenePosOnMap.min.x
                const height = scenePosOnMap.max.y - scenePosOnMap.min.y

                el.style.left = (scenePosOnMap.min.x + (obj.x / sceneWorldBounds.size.x) * width) * 30 + 'px'
                el.style.top = -(scenePosOnMap.min.y + (obj.y / sceneWorldBounds.size.y) * height) * 30 + 'px'

                window.root.append(el)
            }
        }
    }

    console.log('done')
}

refresh()

const controls = [
    window.showFreed,
    window.showRest,
    window.showLamps,
    window.showEnemies,
    window.showBeam,
    window.showChandelier,
    window.showSeer,
]
controls.forEach(it => it?.addEventListener('change', () => refresh()))

function showOverlay() {
    window.overlay.style.visibility = 'visible'
    window.overlay.style.opacity = '1'
}
function hideOverlay() {
    window.overlay.style.visibility = ''
    window.overlay.style.opacity = ''
}

let lastTarget
window.addEventListener("dragenter", function(event) {
    event.preventDefault()
    lastTarget = event.target
    showOverlay()
})

window.addEventListener('dragleave', function(event) {
    event.preventDefault()
    if(event.target === lastTarget || event.target === document) hideOverlay()
})

window.addEventListener("dragover", function (e) { e.preventDefault(); });

window.addEventListener('drop', function(ev) {
    ev.preventDefault();
    hideOverlay()
    loadFromListFiles(ev.dataTransfer.files)
})

window.input.addEventListener('change', it => loadFromListFiles(it.target.files))

async function loadFromListFiles(files) {
    currentItems = {}
    const file = files[0]
    const data = JSON.parse(await file.text())
    const items = data.modData.LumaflyKnight.items2
    for(const sk in items) {
        const res = {}
        items[sk].forEach(it => res[it] = true)
        currentItems[sk] = res
    }
    refresh()
    console.log(currentItems)
}
