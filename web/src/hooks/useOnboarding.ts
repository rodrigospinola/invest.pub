import { useState, useCallback } from 'react';
import type { ChatMessage, AllocationPreviewItem } from '../types/chat';
import type { Profile } from '../types/profile';
import { chatService } from '../services/chatService';
import { profileService } from '../services/profileService';

export function useOnboarding() {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [profile, setProfile] = useState<Profile | null>(null);
  const [error, setError] = useState('');
  const [suggestedReplies, setSuggestedReplies] = useState<string[]>([]);
  const [allocationPreview, setAllocationPreview] = useState<AllocationPreviewItem[] | null>(null);
  const [profileSaved, setProfileSaved] = useState(false); // flag for fallback CTA

  const sendMessage = useCallback(async (text: string) => {
    setIsLoading(true);
    setError('');
    setSuggestedReplies([]);

    const userMessage: ChatMessage = { role: 'user', content: text };
    setMessages(prev => [...prev, userMessage]);

    let response;
    try {
      // ── 1. Call the chat API ─────────────────────────────────────────────
      response = await chatService.sendMessage(text, 'onboarding', messages);
    } catch {
      // Only on a real network/API failure: show error and undo the user message
      setError('Erro ao conectar com o assistente. Tente novamente.');
      setMessages(prev => prev.slice(0, -1));
      setIsLoading(false);
      return;
    }

    // ── 2. Update chat UI (always, regardless of what comes next) ──────────
    setMessages(
      response.history.map(m => ({ role: m.role as 'user' | 'assistant', content: m.content })),
    );
    setSuggestedReplies(response.suggestedReplies ?? []);
    if (response.allocationPreview) setAllocationPreview(response.allocationPreview);
    setIsLoading(false);

    // ── 3. Navigate when profile is saved ────────────────────────────────
    if (response.toolsCalled?.includes('save_profile')) {
      setProfileSaved(true); // show fallback CTA immediately
      await tryLoadProfile(setProfile);
    }
  }, [messages]);

  return { messages, isLoading, profile, error, sendMessage, suggestedReplies, allocationPreview, profileSaved };
}

/** Attempts to fetch the saved profile up to 3 times, with back-off. */
async function tryLoadProfile(setProfile: (p: Profile) => void) {
  const delays = [0, 600, 1200];
  for (const delay of delays) {
    if (delay > 0) await sleep(delay);
    try {
      const saved = await profileService.getProfile();
      setProfile(saved);
      return; // success
    } catch {
      // retry on next iteration
    }
  }
  // If all retries fail, the user can still use the fallback CTA button
}

function sleep(ms: number) {
  return new Promise<void>(r => setTimeout(r, ms));
}
