import type { Component } from 'vue'
import type { BlockType } from '@/studio/types'
import BadgeBlock from './BadgeBlock.vue'
import ButtonBlock from './ButtonBlock.vue'
import CarouselBlock from './CarouselBlock.vue'
import DividerBlock from './DividerBlock.vue'
import FaqBlock from './FaqBlock.vue'
import FeaturesBlock from './FeaturesBlock.vue'
import FormBlock from './FormBlock.vue'
import GalleryBlock from './GalleryBlock.vue'
import HeadingBlock from './HeadingBlock.vue'
import ImageBlock from './ImageBlock.vue'
import MapBlock from './MapBlock.vue'
import NavBlock from './NavBlock.vue'
import SpacerBlock from './SpacerBlock.vue'
import TestimonialsBlock from './TestimonialsBlock.vue'
import TextBlock from './TextBlock.vue'
import VideoBlock from './VideoBlock.vue'

/** The component that draws each block type, on the canvas and (later) on the published site. */
export const blockComponents: Record<BlockType, Component> = {
  heading: HeadingBlock,
  text: TextBlock,
  button: ButtonBlock,
  badge: BadgeBlock,
  image: ImageBlock,
  gallery: GalleryBlock,
  video: VideoBlock,
  divider: DividerBlock,
  spacer: SpacerBlock,
  form: FormBlock,
  map: MapBlock,
  carousel: CarouselBlock,
  faq: FaqBlock,
  testimonials: TestimonialsBlock,
  features: FeaturesBlock,
  nav: NavBlock,
}
