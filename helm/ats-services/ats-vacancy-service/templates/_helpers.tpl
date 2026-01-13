{{- define "ats-vacancy-service.name" -}}
ats-vacancy-service
{{- end }}

{{- define "ats-vacancy-service.fullname" -}}
{{ printf "%s" (include "ats-vacancy-service.name" .) }}
{{- end }}
