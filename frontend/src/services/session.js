import { setStoredUser } from './api';
import { unregisterPushNotification } from './pushNotification';

export function signOut() {
  // Clear credentials immediately, including when browser push cleanup hangs or fails.
  setStoredUser(null);
  return unregisterPushNotification({ localOnly: true }).then(() => true, () => false);
}
