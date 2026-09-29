<template>
  <button
    v-if="plain"
    type="button"
    class="like-plain"
    :class="{ 'like-plain--liked': liked }"
    :title="canLike ? (liked ? 'Unlike' : 'Like') : 'Sign in to like'"
    :aria-label="`${liked ? 'Unlike' : 'Like'}, ${count} likes`"
    :aria-pressed="liked"
    @click="onClick"
  >
    <v-icon size="18">{{ liked ? 'mdi-heart' : 'mdi-heart-outline' }}</v-icon>
    <span class="like-plain__count">{{ count }}</span>
  </button>
  <span v-else class="like-group">
    <button
      type="button"
      class="like-btn"
      :class="{ 'like-btn--liked': liked }"
      :title="canLike ? (liked ? 'Unlike' : 'Like') : 'Sign in to like'"
      :aria-label="liked ? 'Unlike' : 'Like'"
      :aria-pressed="liked"
      @click="onClick"
    >
      <v-icon size="18">{{ liked ? 'mdi-heart' : 'mdi-heart-outline' }}</v-icon>
    </button>
    <span class="like-count" :title="`${count} likes`">{{ count }}</span>
  </span>
</template>

<script setup>
// Heart toggle and count. Parent owns the request and passes the new state in.
// Plain drops the boxes for inline use in running text.
const props = defineProps({
  liked: { type: Boolean, default: false },
  count: { type: Number, default: 0 },
  canLike: { type: Boolean, default: false },
  plain: { type: Boolean, default: false },
})
const emit = defineEmits(['toggle'])

const onClick = () => {
  if (props.canLike) emit('toggle')
}
</script>

<style scoped>
.like-group {
  display: inline-flex;
  align-items: center;
}

.like-btn {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  height: 30px;
  padding: 0 12px;
  background: var(--bg);
  border: 1px solid var(--white);
  color: var(--white);
  font: inherit;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  line-height: 1;
  transition:
    background-color var(--dur-fast) var(--ease),
    color var(--dur-fast) var(--ease);
}

.like-btn--liked {
  background: var(--white);
  color: var(--bg);
}

.like-count {
  display: inline-flex;
  align-items: center;
  height: 30px;
  padding: 0 10px;
  border: 1px solid var(--white);
  border-left: 0;
  font-family: var(--font-mono);
  font-size: 13px;
  font-weight: 600;
}

.like-plain {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 0;
  background: none;
  border: 0;
  color: inherit;
  font: inherit;
  cursor: pointer;
  line-height: 1;
  transition: color var(--dur-fast) var(--ease);
}

.like-plain:hover,
.like-plain--liked {
  color: var(--tx);
}

.like-plain__count {
  font-family: var(--font-mono);
}
</style>
