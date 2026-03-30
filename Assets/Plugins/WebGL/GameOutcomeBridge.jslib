mergeInto(LibraryManager.library, {
  gameWin: function() {
    try {
      if (typeof window !== "undefined") {
        if (typeof window.gameWin === "function") {
          window.gameWin();
        }
      }
    } catch (e) {}
  },

  gameLose: function() {
    try {
      if (typeof window !== "undefined") {
        if (typeof window.gameLose === "function") {
          window.gameLose();
        }
      }
    } catch (e) {}
  }
});
