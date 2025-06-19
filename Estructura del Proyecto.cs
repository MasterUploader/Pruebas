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

    const estaLeido = thread.isUnread() === false;
    const estaEnRecibidos = thread.hasLabel(GmailApp.getUserLabelByName("INBOX"));

    if (diferenciaDias >= diasLimite && estaLeido && estaEnRecibidos) {
      thread.moveToArchive(); // Lo quita de Recibidos, pero mantiene la etiqueta
    }
  }
}
