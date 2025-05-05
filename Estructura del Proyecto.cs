    List<VideoModel> ListarVideos(string codcco);
    bool ActualizarVideo(VideoModel video);
    bool EliminarVideo(int codVideo, string codcco);
    bool TieneDependencias(string codcco, int codVideo);
    bool EliminarArchivoFisico(string rutaArchivo);
