import { useEffect, useState } from 'react';
import { bootstrapStoredSession } from '../../../services/auth';
import { getStoredAuthState, subscribeToAuthChanges } from '../../../services/interceptor';
import { canAccessAdmin, canAccessTenant } from '../types';

function readSession() {
  const session = getStoredAuthState();
  const needsPermissions = session.user && (canAccessAdmin(session.user) || canAccessTenant(session.user));
  const needsBootstrap = session.isAuthenticated && (
    !session.user ||
    (needsPermissions && (
      session.permissions.length === 0 ||
      (canAccessTenant(session.user) && session.memberships.length === 0)
    ))
  );

  return {
    ...session,
    needsBootstrap,
  };
}

export function useAuthSession() {
  const [session, setSession] = useState(readSession);
  const [ready, setReady] = useState(() => !readSession().needsBootstrap);

  useEffect(() => subscribeToAuthChanges(() => {
    const next = readSession();
    setSession(next);
    setReady(!next.needsBootstrap);
  }), []);

  useEffect(() => {
    if (!session.needsBootstrap) {
      setReady(true);
      return undefined;
    }

    let active = true;
    setReady(false);

    bootstrapStoredSession()
      .catch(() => null)
      .finally(() => {
        if (!active) {
          return;
        }

        const next = readSession();
        setSession(next);
        setReady(true);
      });

    return () => {
      active = false;
    };
  }, [session.needsBootstrap]);

  return {
    ...session,
    isReady: ready,
  };
}
