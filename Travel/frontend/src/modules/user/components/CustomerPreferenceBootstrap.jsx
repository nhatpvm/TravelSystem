import { useEffect } from 'react';
import { useAuthSession } from '../../auth/hooks/useAuthSession';
import { getCustomerAccountPreferences } from '../../../services/customerCommerceService';
import {
  applyStoredCustomerPreferences,
  saveCustomerPreferences,
} from '../../../services/customerPreferences';

export default function CustomerPreferenceBootstrap() {
  const session = useAuthSession();

  useEffect(() => {
    applyStoredCustomerPreferences();
  }, []);

  useEffect(() => {
    if (!session.isReady || !session.isAuthenticated) {
      return undefined;
    }

    let active = true;

    getCustomerAccountPreferences()
      .then((response) => {
        if (!active) {
          return;
        }

        saveCustomerPreferences(response);
      })
      .catch(() => {
        // Keep last known local preferences when the request fails.
      });

    return () => {
      active = false;
    };
  }, [session.isAuthenticated, session.isReady, session.user?.userId]);

  return null;
}
