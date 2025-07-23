<template>
  <div class="center">
    <h1>{{ httpCode }}</h1>
    <h2>{{ reason }}</h2>
    <p>{{ extra }}</p>
  </div>
</template>

<script setup>
const props = defineProps({
  httpCode: {
    type: String,
    default: '404 Error',
  },
  reason: {
    type: String,
    default: 'Page not found!',
  },
  extra: {
    type: String,
    default: '',
  }
})
</script>

<style scoped>
.center {
  margin: 0 auto;
  text-align: center;
}

h1 {
  font-size: 6em;
  margin-top: 2em;
}

h2 {
  margin-top: -1em;
}

p {
  margin-top: 3em;
}
</style>