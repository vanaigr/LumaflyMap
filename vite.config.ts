import { defineConfig } from 'vite'

export default defineConfig(({ command, mode, isSsrBuild, isPreview }) => {
    return {
        base: '/LumaflyMap',
    }
})
