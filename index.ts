import list from './list.json'
import positions from './positions.json'

console.log(list, positions)

let minX = Infinity, minY = Infinity
let maxX = -Infinity, maxY = -Infinity

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

for(const sceneName in list) {
    const objs = list[sceneName]
    const scenePosOnMap = positions.mapScenes[sceneName]
    if(scenePosOnMap == null) continue

    const objsInWorld = positions.objs[sceneName]
    const sceneWorldBounds = objsInWorld.sceneBounds
    for(const path in objsInWorld.positions) {
        const obj = objsInWorld.positions[path]

        const el = document.createElement('div')
        el.append(document.createElement('div'))
        el.classList.add('marker')

        el.style.left = ((scenePosOnMap.origin.x - scenePosOnMap.size.x * 0.5)
            + (obj.x / sceneWorldBounds.size.x) * scenePosOnMap.size.x) * 30 + 'px'

        el.style.top = -((scenePosOnMap.origin.y - scenePosOnMap.size.y * 0.5)
            + (obj.y / sceneWorldBounds.size.y) * scenePosOnMap.size.y) * 30 + 'px'

        window.root.append(el)
    }
}

console.log('done')
