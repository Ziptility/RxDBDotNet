// example/livedocs-client/src/utils/motionSystem.ts
import { type MotionProps, AnimatePresence } from 'framer-motion';

// Easing functions
const easing = {
  easeInOut: [0.4, 0, 0.2, 1],
  easeOut: [0, 0, 0.2, 1],
  easeIn: [0.4, 0, 1, 1],
};

// Duration values in milliseconds
const duration = {
  shortest: 150,
  shorter: 200,
  short: 250,
  standard: 300,
  complex: 375,
  enteringScreen: 225,
  leavingScreen: 195,
};

// Motion variants for different animation types
export const motionVariants = {
  fadeIn: {
    initial: { opacity: 0 },
    animate: { opacity: 1 },
    exit: { opacity: 0 },
    transition: { duration: duration.shorter / 1000, easing: easing.easeInOut },
  },
  slideInFromRight: {
    initial: { x: 20, opacity: 0 },
    animate: { x: 0, opacity: 1 },
    exit: { x: 20, opacity: 0 },
    transition: { duration: duration.standard / 1000, easing: easing.easeOut },
  },
  slideInFromBottom: {
    initial: { y: 20, opacity: 0 },
    animate: { y: 0, opacity: 1 },
    exit: { y: 20, opacity: 0 },
    transition: { duration: duration.standard / 1000, easing: easing.easeOut },
  },
  scale: {
    initial: { scale: 0.9, opacity: 0 },
    animate: { scale: 1, opacity: 1 },
    exit: { scale: 0.9, opacity: 0 },
    transition: { duration: duration.shorter / 1000, easing: easing.easeInOut },
  },
};

// Reusable motion props for common animations
export const motionProps: Record<string, MotionProps> = {
  fadeIn: motionVariants.fadeIn,
  slideInFromRight: motionVariants.slideInFromRight,
  slideInFromBottom: motionVariants.slideInFromBottom,
  scale: motionVariants.scale,
};

// Staggered children animation
export const staggeredChildren = {
  initial: { opacity: 0 },
  animate: { opacity: 1 },
  exit: { opacity: 0 },
  transition: { staggerChildren: 0.1, delayChildren: 0.2 },
};

export { AnimatePresence };

// Helper function to create custom transitions
export const createCustomTransition = (
  durationMs: number,
  easingFunction: [number, number, number, number] = [0.4, 0, 0.2, 1]
): MotionProps['transition'] => ({
  duration: durationMs / 1000,
  ease: easingFunction,
});
