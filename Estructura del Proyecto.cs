function archivarInvitacionesMeetViejas() {
  const etiquetaNombre = "Invitaciones Meet";
  const diasLimite = 7;

  const etiqueta = GmailApp.getUserLabelByName(etiquetaNombre);
  if (!etiqueta) return;

  const threads = etiqueta.getThreads();
  const ahora = new Date();

  for (const thread of threads) {
    const ultimaFecha = thread.getLastMessageDate();
    const diferenciaDias = Math.floor((ahora - ultimaFecha) / (1000 * 60 * 60 * 24));

    const estaLeido = !thread.isUnread();
    
    // ✅ Verificamos si una de las etiquetas del hilo es "INBOX"
    const tieneInbox = thread.getLabels().some(label => label.getName().toUpperCase() === "INBOX");

    if (diferenciaDias >= diasLimite && estaLeido && tieneInbox) {
      thread.moveToArchive(); // Archiva pero mantiene la etiqueta
    }
  }
}
