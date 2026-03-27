mergeInto(LibraryManager.library, {
  SS_SetInt: function(keyPtr, value) {
    var key = UTF8ToString(keyPtr);
    try {
      sessionStorage.setItem(key, value.toString());
    } catch (e) {}
  },

  SS_GetInt: function(keyPtr, defaultValue) {
    var key = UTF8ToString(keyPtr);
    try {
      var value = sessionStorage.getItem(key);
      if (value === null) {
        return defaultValue;
      }

      var parsed = parseInt(value, 10);
      return isNaN(parsed) ? defaultValue : parsed;
    } catch (e) {
      return defaultValue;
    }
  },

  SS_HasItem: function(keyPtr) {
    var key = UTF8ToString(keyPtr);
    try {
      return sessionStorage.getItem(key) === null ? 0 : 1;
    } catch (e) {
      return 0;
    }
  },

  SS_RemoveItem: function(keyPtr) {
    var key = UTF8ToString(keyPtr);
    try {
      sessionStorage.removeItem(key);
    } catch (e) {}
  }
});
